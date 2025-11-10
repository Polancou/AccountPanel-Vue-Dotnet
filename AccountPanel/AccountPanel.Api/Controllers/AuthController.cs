using Asp.Versioning;
using AccountPanel.Application.DTOs;
using AccountPanel.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

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
public class AuthController(IAuthService authService) : ControllerBase
{
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
        var tokenResponse = await authService.LoginAsync(loginDto);
        // Devuelve el token de acceso y el refresh token
        return Ok(tokenResponse);
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
        // Se delega la lógica de login externo al servicio de autenticación.
        var tokenResponse = await authService.ExternalLoginAsync(externalLoginDto);
        // Devuelve el token de acceso y el refresh token
        return Ok(tokenResponse);
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
        // Se delega la lógica de refresco al servicio de autenticación
        var tokenResponse = await authService.RefreshTokenAsync(refreshTokenRequest.RefreshToken);
        // Devuelve el token de acceso y el refresh token
        return Ok(tokenResponse);
    }
}