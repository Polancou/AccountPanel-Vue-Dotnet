using AccountPanel.Application.DTOs;
using AccountPanel.Domain.Models;
using Microsoft.AspNetCore.Http;

namespace AccountPanel.Application.Interfaces;

/// <summary>
/// Define el contrato para los servicios relacionados con el perfil de usuario.
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Obtiene el perfil de un usuario por su ID.
    /// </summary>
    /// <param name="userId">El ID del usuario a buscar.</param>
    /// <returns>Un DTO del perfil del usuario o null si no se encuentra.</returns>
    Task<PerfilUsuarioDto> GetProfileByIdAsync(int userId);

    /// <summary>
    /// Actualiza el perfil de un usuario existente.
    /// </summary>
    /// <param name="userId">El ID del usuario a actualizar.</param>
    /// <param name="perfilDto">Los nuevos datos para el perfil.</param>
    /// <returns>True si la actualización fue exitosa, false si el usuario no fue encontrado.</returns>
    Task<bool> ActualizarPerfilAsync(int userId, ActualizarPerfilDto perfilDto);

    /// <summary>
    /// Cambia la contraseña de un usuario.
    /// </summary>
    /// <param name="userId">El ID del usuario a cambiar la contraseña.</param>
    /// <param name="cambioPasswordDto">Los datos para el cambio de contraseña.</param>
    /// <returns>True si el cambio de contraseña fue exitoso, false si el usuario no fue encontrado.</returns>
    Task<AuthResult> CambiarPasswordAsync(int userId, CambiarPasswordDto dto);
    /// <summary>
    /// Sube una imagen de perfil para un usuario.
    /// </summary>
    /// <param name="userId">El ID del usuario a actualizar.</param>
    /// <param name="file">El archivo a subir.</param>
    /// <returns></returns>
    Task<string> UploadAvatarAsync(int userId, IFormFile file);
}