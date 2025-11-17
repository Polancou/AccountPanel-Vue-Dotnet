using AccountPanel.Application.DTOs;
using AccountPanel.Domain.Models;

namespace AccountPanel.Application.Interfaces;

public interface IAdminService
{
    /// <summary>
    /// Obtiene todos los perfiles de usuarios en el sistema
    /// </summary>
    /// <param name="pageNumber">Número de página a mostrar</param>
    /// <param name="pageSize">Tamaño de página a mostrar</param>
    /// <returns>Una lista de perfiles de usuarios</returns>
    Task<PagedResultDto<PerfilUsuarioDto>> GetUsersPaginatedAsync(int pageNumber, int pageSize, string? searchTerm = null, RolUsuario? rol = null);

    /// <summary>
    /// Elimina un usuario por su ID.
    /// </summary>
    /// <param name="userId">El ID del usuario a eliminar.</param>
    Task DeleteUserAsync(int userId, int currentAdminId);

    /// <summary>
    /// Actualiza el rol de un usuario.
    /// </summary>
    /// <param name="userIdToUpdate">El ID del usuario a actualizar.</param>
    /// <param name="newRole">El nuevo rol del usuario.</param>
    /// <param name="currentAdminId">El ID del administrador que realiza la operación.</param>
    Task SetUserRoleAsync(int userIdToUpdate, RolUsuario newRole, int currentAdminId);
}
