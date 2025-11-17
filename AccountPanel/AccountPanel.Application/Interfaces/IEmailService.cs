namespace AccountPanel.Application.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Envía un correo electrónico de forma asíncrona.
    /// </summary>
    /// <param name="toEmail">La dirección de email del destinatario.</param>
    /// <param name="subject">El asunto del correo.</param>
    /// <param name="htmlBody">El contenido del correo en formato HTML.</param>
    Task SendEmailAsync(string toEmail, string subject, string htmlBody);
}
