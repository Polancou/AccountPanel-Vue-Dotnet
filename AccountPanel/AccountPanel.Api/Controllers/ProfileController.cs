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

        if (perfilDto == null)
        {
            return NotFound(new { message = "Usuario no encontrado." });
        }
        
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
        var success = await profileService.ActualizarPerfilAsync(UserId, perfilDto);

        if (!success)
        {
            return NotFound(new { message = "Usuario no encontrado." });
        }

        // 204 No Content es la respuesta HTTP estándar y correcta para una
        // actualización exitosa que no necesita devolver ningún dato.
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

        if (!result.Success)
        {
            // Si falla (ej. contraseña antigua incorrecta), devuelve 400
            return BadRequest(new { message = result.Message });
        }

        // Devuelve 200 OK con el mensaje de éxito
        return Ok(new { message = result.Message });
    }
}