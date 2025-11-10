using AccountPanel.Domain.Models;

namespace AccountPanel.Application.Interfaces;

/// <summary>
/// Define el contrato para los servicios que generan tokens de autenticación.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Crea un token de autenticación JWT para un usuario específico.
    /// </summary>
    /// <param name="usuario">La entidad del usuario para quien se generará el token.</param>
    /// <returns>Una cadena que representa el token JWT firmado.</returns>
    string CrearToken(Usuario usuario);
    /// <summary>
    /// Genera un token de actualización (Refresh Token) aleatorio y seguro.
    /// </summary>
    /// <returns>Un string aleatorio para el refresh token.</returns>
    string GenerarRefreshToken();
}