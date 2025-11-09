using AccountPanel.Application.DTOs;

namespace AccountPanel.Application.Interfaces;

public interface IAdminService
{
    /// <summary>
    /// Obtiene todos los perfiles de usuarios en el sistema
    /// </summary>
    /// <param name="pageNumber">Número de página a mostrar</param>
    /// <param name="pageSize">Tamaño de página a mostrar</param>
    /// <returns>Una lista de perfiles de usuarios</returns>
    Task<PagedResultDto<PerfilUsuarioDto>> GetUsersPaginatedAsync(int pageNumber, int pageSize);
}
