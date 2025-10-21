using BaseApp.Application.DTOs;
using BaseApp.Domain.Models;

namespace BaseApp.Application.Interfaces;

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
    Task<AuthResult> LoginAsync(LoginUsuarioDto loginDto);

    /// <summary>
    /// Autentica a un usuario utilizando un token de un proveedor externo (ej. Google).
    /// </summary>
    /// <param name="externalLoginDto">DTO con el nombre del proveedor y el token de ID.</param>
    /// <returns>Un AuthResult que contiene el token JWT si la autenticación es exitosa.</returns>
    Task<AuthResult> ExternalLoginAsync(ExternalLoginDto externalLoginDto);
}