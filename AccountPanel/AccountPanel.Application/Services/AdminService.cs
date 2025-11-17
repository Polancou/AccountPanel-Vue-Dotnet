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
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ValidationException("Este usuario fue modificado por otra persona. Por favor, recarga la página e intenta de nuevo.");
        }
    }

    /// <summary>
    /// Obtiene todos los perfiles de usuarios en el sistema
    /// </summary>
    /// <param name="pageNumber">Número de página a mostrar</param>
    /// <param name="pageSize">Tamaño de página a mostrar</param>
    /// <param name="searchTerm">Término de búsqueda (opcional) para filtrar por nombre o email.</param>
    /// <param name="rol">Rol de usuario (opcional) para filtrar.</param>
    /// <returns>Una lista de perfiles de usuarios</returns>
    public async Task<PagedResultDto<PerfilUsuarioDto>> GetUsersPaginatedAsync(
        int pageNumber, int pageSize, string? searchTerm = null, RolUsuario? rol = null)
    {
        // Innicializamos una variable de tipo IQueryable<Usuario> que será la consulta base.
        IQueryable<Usuario> query = context.Usuarios;

        // Aplicamos el filtro de búsqueda
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerSearchTerm = searchTerm.Trim().ToLower();
            query = query.Where(u =>
                u.NombreCompleto.ToLower().Contains(lowerSearchTerm) ||
                u.Email.ToLower().Contains(lowerSearchTerm)
            );
        }

        // Aplicamos el filtro de Rol
        if (rol.HasValue)
        {
            query = query.Where(u => u.Rol == rol.Value);
        }

        // Obtenemos el conteo total después de aplicar los filtros.
        var totalCount = await query.CountAsync();

        // Aplicamos el orden y la paginación a la consulta filtrada.
        var usuarios = await query
                .OrderBy(u => u.NombreCompleto)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

        // Mapeo de los resultados
        var usersDto = mapper.Map<List<PerfilUsuarioDto>>(usuarios);

        // Se devuelve el resultado con los perfiles de usuarios.
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
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ValidationException("Este usuario fue modificado por otra persona. Por favor, recarga la página e intenta de nuevo.");
        }
    }
}
