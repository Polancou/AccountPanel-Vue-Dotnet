using AccountPanel.Application.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace AccountPanel.Infrastructure.Services;

/// <summary>
/// Clase auxiliar para leer la configuración
/// </summary>
public class SmtpSettings
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string FromEmail { get; set; }
    public string FromName { get; set; }
}

public class SmtpEmailService(IConfiguration configuration) : IEmailService
{
    // Configuración de Mailtrap
    private readonly SmtpSettings _settings = configuration.GetSection("SmtpSettings").Get<SmtpSettings>();
    
    // Instancia del cliente SMTP
    public async Task SendVerificationEmailAsync(string toEmail, string userName, string verificationLink)
    {
        var emailSubject = "¡Bienvenido a AccountPanel! Confirma tu email";

        // 1. Lee la plantilla
        var templatePath = Path.Combine(AppContext.BaseDirectory, "EmailTemplates", "VerificationEmail.html");
        var htmlBody = await File.ReadAllTextAsync(templatePath);

        // 2. Reemplaza placeholders
        htmlBody = htmlBody.Replace("{{UserName}}", userName);
        htmlBody = htmlBody.Replace("{{Link}}", verificationLink);

        // 3. Envía el email (usando un nuevo método helper privado)
        await SendEmailInternalAsync(toEmail, emailSubject, htmlBody);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetLink)
    {
        var emailSubject = "Restablece tu contraseña de AccountPanel";
        
        var templatePath = Path.Combine(AppContext.BaseDirectory, "EmailTemplates", "PasswordResetEmail.html");
        var htmlBody = await File.ReadAllTextAsync(templatePath);
        
        htmlBody = htmlBody.Replace("{{UserName}}", userName);
        htmlBody = htmlBody.Replace("{{Link}}", resetLink);

        await SendEmailInternalAsync(toEmail, emailSubject, htmlBody);
    }

    private async Task SendEmailInternalAsync(string toEmail, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        // Usamos la configuración genérica
        message.From.Add(new MailboxAddress(_settings.FromName ?? "AccountPanel", _settings.FromEmail));
        message.To.Add(new MailboxAddress(toEmail, toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        try
        {
            // Conexión agnóstica (funciona para Mailtrap y AWS SES)
            await client.ConnectAsync(_settings.Host, _settings.Port, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
            await client.SendAsync(message);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw;
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }
}