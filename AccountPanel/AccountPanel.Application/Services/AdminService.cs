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
    /// <returns>Una lista de perfiles de usuarios</returns>
    public async Task<IEnumerable<PerfilUsuarioDto>> GetAllUsersAsync()
    {
        // Obtenemos todos los perfiles de usuarios en el sistema.
        var perfiles = await context.Usuarios.ToListAsync();
        // Convierte los perfiles a DTO seguro para la respuesta.
        return mapper.Map<IEnumerable<PerfilUsuarioDto>>(perfiles);
    }
}
