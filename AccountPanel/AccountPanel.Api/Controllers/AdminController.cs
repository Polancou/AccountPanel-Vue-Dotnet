using System.Security.Claims;
using AccountPanel.Application.DTOs;
using AccountPanel.Application.Interfaces;
using AccountPanel.Domain.Models;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountPanel.Api.Controllers;

/// <summary>
/// Gestión de la API de administración del sistema.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize(Roles = "Admin")]
public class AdminController(IAdminService adminService) : ControllerBase
{
    /// <summary>
    /// Propiedad privada de conveniencia para obtener de forma segura el ID del usuario
    /// autenticado a partir de los claims del token JWT.
    /// Esto evita repetir la misma lógica en cada método del controlador.
    /// </summary>
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

    /// <summary>
    /// Obtiene todos los perfiles de usuarios en el sistema en formato paginado.
    /// </summary>
    /// <param name="pageNumber">Número de página a mostrar</param>
    /// <param name="pageSize">Tamaño de página a mostrar</param>
    /// <param name="searchTerm">Término de búsqueda (opcional) para filtrar por nombre o email.</param>
    /// <param name="rol">Rol de usuario (opcional) para filtrar.</param>
    /// <returns></returns>
    [HttpGet("users")]
    public async Task<IActionResult> GetPAginatedUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] RolUsuario? rol = null)
    {
        // Obtenemos todos los usuarios en el sistema, pasando los filtros.
        var result = await adminService.GetUsersPaginatedAsync(
            pageNumber: pageNumber,
            pageSize: pageSize,
            searchTerm: searchTerm,
            rol: rol);
        // Devolvemos la lista de usuarios como respuesta.
        return Ok(result);
    }

    /// <summary>
    /// Elimina un usuario del sistema.
    /// </summary>
    /// <param name="userId">El ID del usuario a eliminar.</param>
    /// <returns>True si se eliminó correctamente, false si no se encontró.</returns>
    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        // Eliminamos el usuario del sistema.
        await adminService.DeleteUserAsync(userId: userId, currentAdminId: UserId);
        // Devolvemos el resultado de la operación.
        return NoContent();
    }

    /// <summary>
    /// Actualiza el rol de un usuario.
    /// </summary>
    /// <param name="userIdToUpdate">El ID del usuario a actualizar.</param>
    /// <param name="dto">El DTO con el nuevo rol.</param>
    /// <returns>True si se actualizó correctamente, false si no se encontró.</returns>
    [HttpPut("users/{userIdToUpdate}/role")]
    public async Task<IActionResult> SetUserRole(int userIdToUpdate, [FromBody] ActualizarRolUsuarioDto dto)
    {
        // Actualizamos el rol del usuario.
        await adminService.SetUserRoleAsync(userIdToUpdate: userIdToUpdate, newRole: dto.Rol, currentAdminId: UserId);
        // Devolvemos el resultado de la operación.
        return NoContent();
    }
}
