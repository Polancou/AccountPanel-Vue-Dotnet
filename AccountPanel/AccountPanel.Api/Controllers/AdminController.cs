using AccountPanel.Application.Interfaces;
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
    /// Obtiene todos los perfiles de usuarios en el sistema.
    /// </summary>
    /// <returns>Una lista de perfiles de usuarios</returns>
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        // Obtenemos todos los usuarios en el sistema.
        var users = await adminService.GetAllUsersAsync();
        // Devolvemos la lista de usuarios como respuesta.
        return Ok(users);
    }
}
