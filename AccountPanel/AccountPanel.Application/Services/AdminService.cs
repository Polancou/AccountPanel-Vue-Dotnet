using AccountPanel.Application.DTOs;
using AccountPanel.Application.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AccountPanel.Application.Services;

public class AdminService(IApplicationDbContext context, IMapper mapper) : IAdminService
{
    /// <summary>
    /// Obtiene todos los perfiles de usuarios en el sistema
    /// </summary>
    /// <param name="pageNumber">Número de página a mostrar</param>
    /// <param name="pageSize">Tamaño de página a mostrar</param>
    /// <returns>Una lista de perfiles de usuarios</returns>
    public async Task<PagedResultDto<PerfilUsuarioDto>> GetUsersPaginatedAsync(int pageNumber, int pageSize)
    {
        // Obtenemos la cantidad de registros totales.
        var totalCount = await context.Usuarios.CountAsync();
        // Obtenemos todos los perfiles de usuarios en el sistema filtrando por página y tamaño.
        var usuarios = await context.Usuarios
                .OrderBy(u => u.NombreCompleto) 
                .Skip((pageNumber - 1) * pageSize) // Salta las páginas anteriores
                .Take(pageSize) // Toma solo los de esta página
                .ToListAsync();
        // Mapeamos los perfiles a DTO seguro para la respuesta.
        var usersDto = mapper.Map<List<PerfilUsuarioDto>>(usuarios);        
        // Devolvemos el resultado con los perfiles de usuarios.
        return new PagedResultDto<PerfilUsuarioDto>
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = usersDto
        };
    }
}
