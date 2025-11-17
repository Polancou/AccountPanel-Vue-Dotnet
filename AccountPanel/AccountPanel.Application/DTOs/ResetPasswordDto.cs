namespace AccountPanel.Application.DTOs;

/// <summary>
/// Representa los datos necesarios para restablecer la contraseña de un usuario.
/// </summary>
public class ResetPasswordDto
{
    public string Token { get; set; }
    public string NewPassword { get; set; }
    public string ConfirmPassword { get; set; }
}
