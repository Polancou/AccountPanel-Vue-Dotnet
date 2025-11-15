using AccountPanel.Application.DTOs;
using AccountPanel.Application.Exceptions;
using AccountPanel.Application.Interfaces;
using AccountPanel.Domain.Models;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AccountPanel.Application.Services;

public class AdminService(IApplicationDbContext context, IMapper mapper) : IAdminService
{
    /// <summary>
    /// Elimina un usuario por su ID.
    /// </summary>
    /// <param name="userId">El ID del usuario a eliminar.</param>
    public async Task DeleteUserAsync(int userId, int currentAdminId)
    {
        // Verificamos que el usuario no sea el administrador.
        if (userId == currentAdminId)
            throw new ValidationException("No se puede eliminar el administrador.");
        // Intentamos obtener el usuario a eliminar.
        var usuario = await context.Usuarios.FindAsync(userId);
        // Si no se encontró, lanza una excepción.
        if (usuario == null) throw new NotFoundException("No se encontró el usuario.");
        // Eliminamos el usuario del sistema.
        context.Usuarios.Remove(usuario);
        // Guardamos los cambios en la base de datos.
        await context.SaveChangesAsync();
    }

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

    /// <summary>
    /// Actualiza el rol de un usuario.
    /// </summary>
    /// <param name="userIdToUpdate">El ID del usuario a actualizar.</param>
    /// <param name="dto">El DTO con el nuevo rol.</param>
    /// <param name="currentAdminId">El ID del administrador que realiza la operación.</param>
    public async Task SetUserRoleAsync(int userIdToUpdate, RolUsuario newRole, int currentAdminId)
    {
        // Verificamos que el usuario no sea el administrador.
        if (userIdToUpdate == currentAdminId)
            throw new ValidationException("No se puede actualizar el rol del administrador.");
        // Intentamos obtener el usuario a actualizar.
        var usuario = await context.Usuarios.FindAsync(userIdToUpdate);
        // Si no se encontró, lanza una excepción.
        if (usuario == null) throw new NotFoundException("No se encontró el usuario.");
        // Actualizamos el rol del usuario.
        usuario.SetRol(rol: newRole);
        // Guardamos los cambios en la base de datos.
        await context.SaveChangesAsync();
    }
}
