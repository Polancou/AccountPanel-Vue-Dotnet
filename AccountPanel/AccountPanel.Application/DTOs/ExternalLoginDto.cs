namespace AccountPanel.Application.DTOs;

/// <summary>
/// Representa los datos para un inicio de sesión con un proveedor externo.
/// Las reglas de validación se definen en ExternalLoginDtoValidator.cs
/// </summary>
public class ExternalLoginDto
{
    public string Provider { get; set; }
    public string IdToken { get; set; }
}