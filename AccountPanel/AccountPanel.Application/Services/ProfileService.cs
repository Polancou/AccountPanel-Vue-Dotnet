using AccountPanel.Application.DTOs;
using AccountPanel.Application.Interfaces;
using AutoMapper;

namespace AccountPanel.Application.Services;

/// <summary>
/// Implementa la lógica de negocio para gestionar los perfiles de usuario.
/// </summary>
/// <param name="context">El contrato del contexto de la base de datos para acceder a los datos.</param>
/// <param name="mapper">El servicio de AutoMapper para convertir entre entidades y DTOs.</param>
public class ProfileService(IApplicationDbContext context, IMapper mapper) : IProfileService
{
    /// <summary>
    /// Obtiene el perfil de un usuario por su ID.
    /// </summary>
    /// <param name="userId">El ID del usuario a buscar.</param>
    /// <returns>Un DTO del perfil del usuario o null si no se encuentra.</returns>
    public async Task<PerfilUsuarioDto> GetProfileByIdAsync(int userId)
    {
        // Busca al usuario en la base de datos a través del contexto.
        var usuario = await context.Usuarios.FindAsync(userId);
        if (usuario == null) return null;

        // Convierte la entidad de dominio a un DTO seguro para la respuesta.
        return mapper.Map<PerfilUsuarioDto>(usuario);
    }
    
    /// <summary>
    /// Actualiza el perfil de un usuario existente.
    /// </summary>
    /// <param name="userId">El ID del usuario a actualizar.</param>
    /// <param name="perfilDto">Los nuevos datos para el perfil.</param>
    /// <returns>True si la actualización fue exitosa, false si el usuario no fue encontrado.</returns>
    public async Task<bool> ActualizarPerfilAsync(int userId, ActualizarPerfilDto perfilDto)
    {
        // Busca al usuario que se desea actualizar.
        var usuario = await context.Usuarios.FindAsync(userId);

        if (usuario == null)
        {
            // Si no se encuentra el usuario, la operación falla.
            return false; 
        }

        // Delega la lógica de la actualización a un método en la propia entidad de dominio.
        usuario.ActualizarPerfil(perfilDto.NombreCompleto, perfilDto.NumeroTelefono);

        // Persiste los cambios en la base de datos.
        await context.SaveChangesAsync();
        return true;
    }
}