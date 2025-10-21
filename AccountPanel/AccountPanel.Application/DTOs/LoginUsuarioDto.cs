namespace AccountPanel.Application.DTOs;

/// <summary>
/// Representa los datos de inicio de sesión del Usuario.
/// Las reglas de validación se definen en LoginUsuarioDtoValidator.cs
/// </summary>
public class LoginUsuarioDto
{
    public string Email { get; set; }
    public string Password { get; set; }
}