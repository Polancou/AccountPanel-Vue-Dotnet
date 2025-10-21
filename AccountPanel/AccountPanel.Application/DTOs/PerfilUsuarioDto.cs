namespace AccountPanel.Application.DTOs;

/// <summary>
/// Representa los datos del perfil de un usuario que son seguros para ser devueltos por la API.
/// </summary>
public class PerfilUsuarioDto
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; }
    public string Email { get; set; }
    public string NumeroTelefono { get; set; }
    public string Rol { get; set; }
    public DateTime FechaRegistro { get; set; }
}