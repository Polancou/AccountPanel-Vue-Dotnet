using System.Security.Claims;
using Asp.Versioning;
using AccountPanel.Application.DTOs;
using AccountPanel.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountPanel.Api.Controllers;

/// <summary>
/// Gestiona las peticiones HTTP relacionadas con el perfil del usuario autenticado.
/// Este es un "controlador delgado" (thin controller): su única responsabilidad es recibir
/// las peticiones web, delegar toda la lógica de negocio al servicio correspondiente
/// (`IProfileService`), y mapear el resultado a una respuesta HTTP.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")] // La ruta base representa el "recurso" de perfil.
[ApiVersion("1.0")]
[Authorize] // El atributo [Authorize] protege todos los endpoints, requiriendo un JWT válido.
public class ProfileController(IProfileService profileService) : ControllerBase
{
    /// <summary>
    /// Propiedad privada de conveniencia para obtener de forma segura el ID del usuario
    /// autenticado a partir de los claims del token JWT.
    /// Esto evita repetir la misma lógica en cada método del controlador.
    /// </summary>
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

    /// <summary>
    /// Obtiene los datos del perfil del usuario actualmente autenticado.
    /// </summary>
    /// <returns>Un 200 OK con el DTO del perfil.</returns>
    /// <returns>Un 200 404 Not Found si el usuario no existe.</returns>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        // Se delega completamente la lógica de negocio al servicio de perfil.
        var perfilDto = await profileService.GetProfileByIdAsync(UserId);
        return Ok(perfilDto);
    }

    /// <summary>
    /// Actualiza los datos del perfil del usuario actualmente autenticado.
    /// </summary>
    /// <param name="perfilDto">Un objeto JSON con los nuevos datos para el perfil.</param>
    /// <returns>Un 204 No Content si la actualización fue exitosa</returns>
    /// <returns>Un 404 Not Found si el usuario no existe.</returns>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] ActualizarPerfilDto perfilDto)
    {
        // Se delega completamente la lógica de actualización al servicio de perfil.
        await profileService.ActualizarPerfilAsync(UserId, perfilDto);
        return NoContent();
    }

    /// <summary>
    /// Cambia la contraseña del usuario autenticado.
    /// </summary>
    /// <param name="dto">Objeto con la contraseña antigua y la nueva.</param>
    /// <returns>Un 200 OK con mensaje de éxito o un 400 Bad Request con mensaje de error.</returns>
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] CambiarPasswordDto dto)
    {
        // Se delega completamente la lógica de cambio de contraseña al servicio de perfil
        var result = await profileService.CambiarPasswordAsync(userId: UserId, dto: dto);
        // Devuelve 200 OK con el mensaje de éxito
        return Ok(new { message = result.Message });
    }

    /// <summary>
    /// Sube o actualiza la imagen de avatar del usuario autenticado.
    /// </summary>
    /// <param name="file">El archivo de imagen enviado como form-data.</param>
    /// <returns>Un 200 OK con la nueva URL del avatar.</returns>
    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file)
    {
        // Valida que se haya enviado un archivo
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No se ha proporcionado ningún archivo." });
        }

        // Valida el tipo de archivo
        if (!file.ContentType.StartsWith("image/"))
        {
            return BadRequest(new { message = "El archivo debe ser de tipo imagen." });
        }

        // Valida el tamaño del archivo
        if (file.Length > 1024 * 1024 * 2)
        {
            return BadRequest(new { message = "El archivo debe ser menor a 2 MB." });
        }

        // Valida el formato del archivo
        var fileExtension = Path.GetExtension(file.FileName);
        if (!fileExtension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) &&
            !fileExtension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) &&
            !fileExtension.Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "El archivo debe ser de formato JPG, JPEG o PNG." });
        }

        // Se delega completamente la lógica de subida de avatar al servicio de perfil
        var newAvatarUrl = await profileService.UploadAvatarAsync(UserId, file);
        // Devuelve 200 OK con la nueva URL del avatar
        return Ok(new { avatarUrl = newAvatarUrl });
    }
}