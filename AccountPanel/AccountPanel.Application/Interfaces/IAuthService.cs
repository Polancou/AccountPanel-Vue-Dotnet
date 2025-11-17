using AccountPanel.Application.DTOs;
using AccountPanel.Domain.Models;

namespace AccountPanel.Application.Interfaces;

/// <summary>
/// Define el contrato para la lógica de negocio de autenticación.
/// Esta interfaz desacopla los controladores de la implementación concreta del servicio.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registra un nuevo usuario en el sistema basado en los datos proporcionados.
    /// </summary>
    /// <param name="registroDto">DTO con la información para el registro.</param>
    /// <returns>Un AuthResult indicando el éxito o fracaso de la operación.</returns>
    Task<AuthResult> RegisterAsync(RegistroUsuarioDto registroDto);
    /// <summary>
    /// Autentica a un usuario utilizando su email y contraseña.
    /// </summary>
    /// <param name="loginDto">DTO con las credenciales de inicio de sesión.</param>
    /// <returns>Un AuthResult que contiene el token JWT si la autenticación es exitosa.</returns>
    Task<TokenResponseDto> LoginAsync(LoginUsuarioDto loginDto);
    /// <summary>
    /// Autentica a un usuario utilizando un token de un proveedor externo (ej. Google).
    /// </summary>
    /// <param name="externalLoginDto">DTO con el nombre del proveedor y el token de ID.</param>
    /// <returns>Un AuthResult que contiene el token JWT si la autenticación es exitosa.</returns>
    Task<TokenResponseDto> ExternalLoginAsync(ExternalLoginDto externalLoginDto);
    /// <summary>
    /// Refresca un access token usando un refresh token válido.
    /// <param name="refreshToken">El token de refresco.</param>
    /// <returns>Un AuthResult que contiene el token JWT si la autenticación es exitosa.</returns>
    /// </summary>
    Task<TokenResponseDto> RefreshTokenAsync(string refreshToken);
    /// <summary>
    /// Verifica el email de un usuario usando el token de verificación.
    /// </summary>
    /// <param name="token">El token enviado al email del usuario.</param>
    /// <returns>Un AuthResult indicando el éxito o fracaso.</returns>
    Task<AuthResult> VerifyEmailAsync(string token);
    /// <summary>
    /// Inicia el proceso de reseteo de contraseña para un email.
    /// </summary>
    /// <param name="email">El email del usuario que olvidó su contraseña.</param>
    Task<AuthResult> ForgotPasswordAsync(string email);
    /// <summary>
    /// Completa el proceso de reseteo de contraseña usando un token.
    /// </summary>
    /// <param name="dto">El DTO con el token y la nueva contraseña.</param>
    Task<AuthResult> ResetPasswordAsync(ResetPasswordDto dto);
}