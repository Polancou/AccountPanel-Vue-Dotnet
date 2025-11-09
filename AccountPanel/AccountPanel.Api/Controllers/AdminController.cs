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
    /// Obtiene todos los perfiles de usuarios en el sistema en formato paginado.
    /// </summary>
    /// <param name="pageNumber">Número de página a mostrar</param>
    /// <param name="pageSize">Tamaño de página a mostrar</param>
    /// <returns></returns>
    [HttpGet("users")]
    public async Task<IActionResult> GetPAginatedUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        // Obtenemos todos los usuarios en el sistema.
        var result = await adminService.GetUsersPaginatedAsync(pageNumber: pageNumber, pageSize: pageSize);
        // Devolvemos la lista de usuarios como respuesta.
        return Ok(result);
    }
}
