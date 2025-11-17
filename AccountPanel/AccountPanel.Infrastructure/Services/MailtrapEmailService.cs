using AccountPanel.Application.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace AccountPanel.Infrastructure.Services;

/// <summary>
/// Clase auxiliar para leer la configuración
/// </summary>
public class MailtrapSettings
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string FromEmail { get; set; }
}

public class MailtrapEmailService(IConfiguration configuration) : IEmailService
{
    // Configuración de Mailtrap
    private readonly MailtrapSettings _settings = configuration.GetSection("MailtrapSettings").Get<MailtrapSettings>();
    
    // Instancia del cliente SMTP
    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        // Creamos el mensaje de correo
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("AccountPanel App", _settings.FromEmail));
        message.To.Add(new MailboxAddress(toEmail, toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };
        
        // Creamos el cliente SMTP
        using var client = new SmtpClient();

        try
        {
            // Conecta al servidor SMTP de Mailtrap
            await client.ConnectAsync(_settings.Host, _settings.Port, MailKit.Security.SecureSocketOptions.StartTls);
            // Autentícate
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
            // Envía el email
            await client.SendAsync(message);
        }
        catch (Exception ex)
        {
            // Log temporal de la excepción
            Console.WriteLine(ex.Message);
            throw;
        }
        finally
        {
            // Cerramos el cliente SMTP
            await client.DisconnectAsync(true);
        }
    }
}