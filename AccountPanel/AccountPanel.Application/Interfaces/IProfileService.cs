using AccountPanel.Application.DTOs;

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
}