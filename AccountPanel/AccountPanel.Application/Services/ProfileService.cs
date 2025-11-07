using AccountPanel.Application.DTOs;
using AccountPanel.Application.Interfaces;
using AccountPanel.Domain.Models;
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
        var usuario = await context.Usuarios.FindAsync(keyValues: userId);

        if (usuario == null)
        {
            // Si no se encuentra el usuario, la operación falla.
            return false; 
        }

        // Delega la lógica de la actualización a un método en la propia entidad de dominio.
        usuario.ActualizarPerfil(nuevoNombre: perfilDto.NombreCompleto, nuevoNumero: perfilDto.NumeroTelefono);

        // Persiste los cambios en la base de datos.
        await context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Cambia la contraseña de un usuario.
    /// </summary>
    /// <param name="userId">El ID del usuario a cambiar la contraseña.</param>
    /// <param name="cambioPasswordDto">Los datos para el cambio de contraseña.</param>
    /// <returns>True si el cambio de contraseña fue exitoso, false si el usuario no fue encontrado.</returns>
    public async Task<AuthResult> CambiarPasswordAsync(int userId, CambiarPasswordDto dto)
    {
        var usuario = await context.Usuarios.FindAsync(keyValues: userId);
        if (usuario == null)
        {
            // Este caso no debería ocurrir si el usuario está autenticado
            return AuthResult.Fail("Usuario no encontrado.");
        }

        // Verifica si el usuario tiene una contraseña local
        if (string.IsNullOrEmpty(usuario.PasswordHash))
        {
            return AuthResult.Fail("No puedes cambiar la contraseña de una cuenta de inicio de sesión externo.");
        }

        // Verifica si la contraseña antigua es correcta
        if (!BCrypt.Net.BCrypt.Verify(text: dto.OldPassword, hash: usuario.PasswordHash))
        {
            return AuthResult.Fail("La contraseña actual es incorrecta.");
        }

        // Hashea y guarda la nueva contraseña
        var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(inputKey: dto.NewPassword);
        usuario.EstablecerPasswordHash(passwordHash: newPasswordHash);

        await context.SaveChangesAsync();

        return AuthResult.Ok(token: null, message: "Contraseña actualizada exitosamente.");
    }
}