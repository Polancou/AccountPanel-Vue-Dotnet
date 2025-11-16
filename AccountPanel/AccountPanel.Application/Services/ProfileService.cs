using AccountPanel.Application.DTOs;
using AccountPanel.Application.Exceptions;
using AccountPanel.Application.Interfaces;
using AccountPanel.Domain.Models;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AccountPanel.Application.Services;

/// <summary>
/// Implementa la lógica de negocio para gestionar los perfiles de usuario.
/// </summary>
/// <param name="context">El contrato del contexto de la base de datos para acceder a los datos.</param>
/// <param name="mapper">El servicio de AutoMapper para convertir entre entidades y DTOs.</param>
public class ProfileService(IApplicationDbContext context, IMapper mapper, IFileStorageService fileStorageService) : IProfileService
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
        if (usuario == null)
            throw new NotFoundException(message: "Usuario no encontrado.");

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
            throw new NotFoundException("Usuario no encontrado.");
        }
        // Delega la lógica de la actualización a un método en la propia entidad de dominio.
        usuario.ActualizarPerfil(nuevoNombre: perfilDto.NombreCompleto, nuevoNumero: perfilDto.NumeroTelefono);

        // Persiste los cambios en la base de datos.
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ValidationException("Este usuario fue modificado por otra persona. Por favor, recarga la página e intenta de nuevo.");
        }
        
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
            return AuthResult.Fail("Usuario no encontrado.");
        }

        // Verifica si el usuario tiene una contraseña local
        if (string.IsNullOrEmpty(usuario.PasswordHash))
        {
            throw new ValidationException("No puedes cambiar la contraseña de una cuenta de inicio de sesión externo.");
        }

        // Verifica si la contraseña antigua es correcta
        if (!BCrypt.Net.BCrypt.Verify(text: dto.OldPassword, hash: usuario.PasswordHash))
        {
            throw new ValidationException("La contraseña actual es incorrecta.");
        }

        // Hashea y guarda la nueva contraseña
        var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(inputKey: dto.NewPassword);
        usuario.EstablecerPasswordHash(passwordHash: newPasswordHash);

        await context.SaveChangesAsync();

        return AuthResult.Ok(token: null, message: "Contraseña actualizada exitosamente.");
    }

    /// <summary>
    /// Sube una imagen de perfil para un usuario.
    /// </summary>
    /// <param name="userId">El ID del usuario a actualizar.</param>
    /// <param name="file">El archivo a subir.</param>
    /// <returns>La URL de la imagen de perfil del usuario.</returns>  
    /// <exception cref="NotImplementedException"></exception>
    public async Task<string> UploadAvatarAsync(int userId, IFormFile file)
    {
        // Busca al usuario en la base de datos a través del contexto.
        var usuario = await context.Usuarios.FindAsync(userId);
        // Si no se encuentra el usuario, la operación falla.
        if (usuario == null)
        {
            throw new NotFoundException("Usuario no encontrado.");
        }
        // Genera un nombre de archivo único para evitar colisiones
        var fileExtension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        // Usa el servicio de almacenamiento para guardar el archivo
        string fileUrl;
        await using (var stream = file.OpenReadStream())
        {
            fileUrl = await fileStorageService.SaveFileAsync(stream, uniqueFileName);
        }
        // Actualiza la entidad Usuario con la nueva URL
        usuario.SetAvatarUrl(fileUrl);
        // Guarda los cambios en la base de datos
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ValidationException("Este usuario fue modificado por otra persona. Por favor, recarga la página e intenta de nuevo.");
        }

        // Devuelve la URL al controlador (y al frontend)
        return fileUrl;
    }
}