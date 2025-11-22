using Asp.Versioning;
using AccountPanel.Application.DTOs;
using AccountPanel.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AccountPanel.Api.Controllers;

/// <summary>
/// Gestiona las peticiones HTTP relacionadas con la autenticación de usuarios.
/// Este es un "controlador delgado" (thin controller), lo que significa que su única
/// responsabilidad es recibir las peticiones y delegar toda la lógica de negocio
/// a la capa de servicio a través de la interfaz IAuthService.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[EnableRateLimiting("AuthPolicy")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private void SetRefreshTokenInCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,   
            Expires = DateTime.UtcNow.AddDays(30), 
            SameSite = SameSiteMode.Strict, // Protege contra CSRF
            Secure = true // Solo se envía por HTTPS (localhost lo permite en dev)
        };
        
        Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
    }
    
    /// <summary>
    /// Endpoint para registrar un nuevo usuario en el sistema.
    /// </summary>
    /// <param name="registroDto">Un objeto JSON con los datos del nuevo usuario (nombre, email, contraseña, etc.).</param>
    /// <returns>
    /// Un resultado 200 OK si el registro es exitoso.
    /// Un resultado 400 Bad Request si los datos son inválidos o el email ya está en uso.
    /// </returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegistroUsuarioDto registroDto)
    {
        // Se delega la lógica de registro al servicio de autenticación.
        var result = await authService.RegisterAsync(registroDto);
        // Si el servicio indica que la operación no fue exitosa, se devuelve un error.
        if (!result.Success) return BadRequest(new { message = result.Message });
        // Si el registro fue exitoso, se devuelve una respuesta afirmativa.
        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Endpoint para el inicio de sesión con email y contraseña.
    /// </summary>
    /// <param name="loginDto">Un objeto JSON con el email y la contraseña del usuario.</param>
    /// <returns>
    /// Un resultado 200 OK con un token JWT si las credenciales son válidas.
    /// Un resultado 401 Unauthorized si las credenciales son incorrectas.
    /// </returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUsuarioDto loginDto)
    {
        // Se delega la lógica de login al servicio de autenticación
        var result = await authService.LoginAsync(loginDto);
        // Guarda el refresh token en la cookie
        SetRefreshTokenInCookie(result.RefreshToken);
        // Devuelve el access token en el JSON
        return Ok(new { accessToken = result.AccessToken });
    }

    /// <summary>
    /// Endpoint para el inicio de sesión con un proveedor externo (ej. Google).
    /// </summary>
    /// <param name="externalLoginDto">Un objeto JSON con el nombre del proveedor y el token de ID obtenido del cliente.</param>
    /// <returns>
    /// Un resultado 200 OK con un token JWT si la autenticación externa es exitosa.
    /// Un resultado 401 Unauthorized si el token del proveedor es inválido o no se puede verificar.
    /// </returns>
    [HttpPost("external-login")]
    public async Task<IActionResult> ExternalLogin([FromBody] ExternalLoginDto externalLoginDto)
    {
        // Se delega la lógica al servicio de autenticación
        var result = await authService.ExternalLoginAsync(externalLoginDto);
        // Guarda el refresh token en la cookie
        SetRefreshTokenInCookie(result.RefreshToken);
        // Devuelve el access token en el JSON
        return Ok(new { accessToken = result.AccessToken });
    }

    /// <summary>
    /// Endpoint para refrescar un token de acceso usando un refresh token.
    /// <param name="refreshTokenRequest">El token de refresco.</param>
    /// <returns>
    /// Un resultado 200 OK con el token de acceso y el refresh token.
    /// Un resultado 400 Bad Request si el token de refresco no es válido.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto refreshTokenRequest)
    {
        // Intenta leer el token desde la cookie
        var refreshToken = Request.Cookies["refreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(new { message = "El token de refresco es requerido." });
        }

        // Llama al servicio
        var result = await authService.RefreshTokenAsync(refreshToken);

        // Rotación de tokens: Actualiza la cookie con el nuevo refresh token
        SetRefreshTokenInCookie(result.RefreshToken);

        return Ok(new { accessToken = result.AccessToken });
    }

    /// <summary>
    /// Endpoint para cerrar la sesión de un usuario.
    /// </summary>
    /// <returns>
    /// Un resultado 200 OK si la operación se ejecuta correctamente, con un mensaje confirmando que la sesión fue cerrada.
    /// </returns>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // Borra la cookie estableciendo una fecha pasada
        Response.Cookies.Delete("refreshToken");
        return Ok(new { message = "Sesión cerrada correctamente." });
    }

    /// <summary>
    /// Endpoint para verificar el email de un usuario.
    /// </summary>
    /// <param name="token">El token enviado al email del usuario.</param>
    /// <returns>
    /// Un resultado 200 OK si el token es válido.
    /// Un resultado 400 Bad Request si el token no es válido.
    [HttpGet("verify-email")] 
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        // Se delega la lógica de verificación de email al servicio de autenticación
        var result = await authService.VerifyEmailAsync(token);
        // Si el servicio indica que la operación no fue exitosa, se devuelve un error.
        if (!result.Success) return BadRequest(new { message = result.Message });
        // Si el registro fue exitoso, se devuelve una respuesta afirmativa.
        return Ok(new { message = result.Message });
    }
    
    /// <summary>
    /// Endpoint para restablecer la contraseña de un usuario.
    /// </summary>
    /// <param name="dto">El DTO con el token y la nueva contraseña.</param>
    /// <returns>
    /// Un resultado 200 OK si la operación fue exitosa.
    /// Un resultado 400 Bad Request si el token no es válido o ha expirado.
    /// Un resultado 404 Not Found si el usuario no existe.
    /// </returns>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        // Se delega la lógica de restablecimiento de contraseña al servicio de autenticación
        var result = await authService.ResetPasswordAsync(dto);
        // Si el servicio indica que la operación no fue exitosa, se devuelve un error.
        if (!result.Success) return BadRequest(new { message = result.Message });
        // Si el registro fue exitoso, se devuelve una respuesta afirmativa.
        return Ok(new { message = result.Message });
    }
    
    /// <summary>
    /// Endpoint que inicia el proceso de restablecimiento de contraseña.
    /// </summary>
    /// <param name="email">El email del usuario que olvidó su contraseña.</param>
    /// <returns>
    /// Un resultado 200 OK si el proceso de restablecimiento fue iniciado correctamente.
    /// Un resultado 400 Bad Request si el email no existe o no se pudo iniciar el proceso.
    /// </returns>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        // Se delega la lógica de restablecimiento de contraseña al servicio de autenticación
        var result = await authService.ForgotPasswordAsync(dto.Email);
        // Si el servicio indica que la operación no fue exitosa, se devuelve un error.
        if (!result.Success) return BadRequest(new { message = result.Message });
        // Si el registro fue exitoso, se devuelve una respuesta afirmativa.
        return Ok(new { message = result.Message });
    }
}