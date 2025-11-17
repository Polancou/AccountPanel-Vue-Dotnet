namespace AccountPanel.Application.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Envía el correo de verificación de email.
    /// </summary>
    /// <param name="toEmail">Email del destinatario.</param>
    /// <param name="userName">Nombre del usuario (para {{UserName}}).</param>
    /// <param name="verificationLink">El enlace de verificación (para {{Link}}).</param>
    Task SendVerificationEmailAsync(string toEmail, string userName, string verificationLink);

    /// <summary>
    /// Envía el correo de reseteo de contraseña.
    /// </summary>
    /// <param name="toEmail">Email del destinatario.</param>
    /// <param name="userName">Nombre del usuario (para {{UserName}}).</param>
    /// <param name="resetLink">El enlace de reseteo (para {{Link}}).</param>
    Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink);
}
