using AccountPanel.Application.DTOs;

namespace AccountPanel.Application.Interfaces;

public interface IAdminService
{
    /// <summary>
    /// Obtiene todos los perfiles de usuarios en el sistema
    /// </summary>
    /// <returns>Una lista de perfiles de usuarios</returns>
    Task<IEnumerable<PerfilUsuarioDto>> GetAllUsersAsync();
}
