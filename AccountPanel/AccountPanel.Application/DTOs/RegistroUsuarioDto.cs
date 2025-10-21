namespace AccountPanel.Application.DTOs;

/// <summary>
/// Define los datos que un cliente debe enviar para registrarse.
/// Las reglas de validación para esta clase se definen en RegistroUsuarioDtoValidator.cs
/// </summary>
public class RegistroUsuarioDto
{
    public string NombreCompleto { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string NumeroTelefono { get; set; }
}