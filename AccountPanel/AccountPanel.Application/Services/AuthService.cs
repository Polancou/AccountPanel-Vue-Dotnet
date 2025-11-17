using AccountPanel.Application.DTOs;
using AccountPanel.Application.Exceptions;
using AccountPanel.Application.Interfaces;
using AccountPanel.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace AccountPanel.Application.Services;

/// <summary>
/// Implementa la lógica de negocio para la autenticación de usuarios.
/// Esta clase orquesta los casos de uso de registro y login, dependiendo de
/// contratos (interfaces) para interactuar con capas externas como la base de datos
/// o los validadores de tokens, sin conocer sus detalles de implementación.
/// </summary>
public class AuthService(
    IApplicationDbContext context,
    ITokenService tokenService,
    IExternalAuthValidator externalAuthValidator,
    IEmailService emailService,
    IConfiguration configuration) : IAuthService
{
    /// <summary>
    /// Registra un nuevo usuario en el sistema.
    /// </summary>
    /// <param name="registroDto">DTO con los datos para el registro.</param>
    /// <returns>Un AuthResult indicando el éxito o fracaso de la operación.</returns>
    public async Task<AuthResult> RegisterAsync(RegistroUsuarioDto registroDto)
    {
        // Verifica si ya existe un usuario con el mismo email para evitar duplicados.
        // Utiliza el contrato IApplicationDbContext para abstraer el acceso a datos.
        var usuarioExistente = await context.Usuarios.AnyAsync(u => u.Email.ToLower() == registroDto.Email.ToLower());
        if (usuarioExistente)
        {
            return AuthResult.Fail("El correo electrónico ya está en uso.");
        }

        // Hashea la contraseña del usuario. Esta es una responsabilidad de la lógica de negocio.
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(registroDto.Password);

        // Crea una nueva instancia de la entidad de dominio Usuario.
        var nuevoUsuario = new Usuario(nombreCompleto: registroDto.NombreCompleto, email: registroDto.Email, numeroTelefono: registroDto.NumeroTelefono,
            rol: RolUsuario.User);
        nuevoUsuario.EstablecerPasswordHash(passwordHash);

        // Añade el nuevo usuario a través del contexto y guarda los cambios.
        await context.Usuarios.AddAsync(nuevoUsuario);
        await context.SaveChangesAsync();
        // Enviar el correo de verificación
        await SendVerificationEmailAsync(nuevoUsuario);
        // Devuelve un resultado exitoso.
        return AuthResult.Ok(null, "Usuario registrado exitosamente.");
    }

    /// <summary>
    /// Autentica a un usuario con su email y contraseña.
    /// </summary>
    /// <param name="loginDto">DTO con las credenciales de inicio de sesión.</param>
    /// <returns>Un AuthResult que contiene el token JWT si la autenticación es exitosa.</returns>
    public async Task<TokenResponseDto> LoginAsync(LoginUsuarioDto loginDto)
    {
        // Busca al usuario por su email a través del contrato del contexto.
        var usuario = await context.Usuarios.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

        // Valida si el usuario existe y si la contraseña proporcionada es correcta.
        if (usuario == null || usuario.PasswordHash == null ||
            !BCrypt.Net.BCrypt.Verify(loginDto.Password, usuario.PasswordHash))
        {
            throw new ValidationException("Credenciales inválidas.");
        }
        // Genera los tokens de acceso y refresco
        var accessToken = tokenService.CrearToken(usuario);
        var refreshToken = tokenService.GenerarRefreshToken();
        // Guardamos el nuevo refresh token en el usuario (expira en 30 días)
        usuario.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(30));
        await context.SaveChangesAsync();

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

    /// <summary>
    /// Autentica a un usuario utilizando un token de un proveedor externo (ej. Google).
    /// </summary>
    /// <param name="externalLoginDto">DTO con el nombre del proveedor y el token de ID.</param>
    /// <returns>Un AuthResult que contiene el token JWT si la autenticación es exitosa.</returns>
    public async Task<TokenResponseDto> ExternalLoginAsync(ExternalLoginDto externalLoginDto)
    {
        // Validar que el proveedor es Google
        if (externalLoginDto.Provider.ToLower() != "google")
        {
            throw new ValidationException("Proveedor no soportado.");
        }
        // Validar que el token de ID sea válido
        var userInfo = await externalAuthValidator.ValidateTokenAsync(externalLoginDto.IdToken);

        if (userInfo == null)
        {
            throw new ValidationException("Token externo inválido.");
        }
        // Comienza la lógica de negocio: buscar si el login externo ya existe
        var userLogin = await context.UserLogins.Include(ul => ul.Usuario)
            .FirstOrDefaultAsync(ul => ul.LoginProvider == "Google" && ul.ProviderKey == userInfo.ProviderSubjectId);
        // Declaramos el usuario
        Usuario usuario;
        // Si el usuario ya se ha logueado antes con este proveedor, obtenemos su usuario
        if (userLogin != null)
        {
            usuario = userLogin.Usuario;
        }
        else
        {
            // Nuevo login, buscar por email o crear nuevo usuario
            usuario = await context.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == userInfo.Email.ToLower());
            // Si el usuario ya se ha logueado antes con este proveedor, simplemente genera un nuevo token.
            if (usuario == null)
            {
                // Creamos un nuevo usuario con la información del proveedor.
                usuario = new Usuario(nombreCompleto: userInfo.Name, email: userInfo.Email, numeroTelefono: "", rol: RolUsuario.User);
                // Asignamos la URL de la foto del usuario.
                usuario.SetAvatarUrl(userInfo.PictureUrl);
                // Marcamos el email como verificado
                usuario.MarkEmailAsVerified();
                // Guardamos el usuario en la base de datos.
                await context.Usuarios.AddAsync(usuario);
                await context.SaveChangesAsync();
            }
            // Se crea el registro del login externo y se asocia con la cuenta de usuario (nueva o existente).
            var nuevoLogin = new UserLogin(
                loginProvider: "Google",
                providerKey: userInfo.ProviderSubjectId,
                usuario: usuario);
            usuario.SetAvatarUrl(userInfo.PictureUrl);
            await context.UserLogins.AddAsync(nuevoLogin);
            await context.SaveChangesAsync();
        }

        // Genera los tokens de acceso y refresco
        var accessToken = tokenService.CrearToken(usuario);
        var refreshToken = tokenService.GenerarRefreshToken();
        // Guardamos el nuevo refresh token en el usuario (expira en 30 días)
        usuario.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(30));
        await context.SaveChangesAsync();
        // Devuelve los tokens
        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

    /// <summary>
    /// Refresca un access token usando un refresh token válido.
    /// <param name="refreshToken">El token de refresco.</param>
    /// <returns>Un AuthResult que contiene el token JWT si la autenticación es exitosa.</returns>
    /// </summary>
    public async Task<TokenResponseDto> RefreshTokenAsync(string refreshToken)
    {
        // Buscamos al usuario que tenga este refresh token
        var usuario = await context.Usuarios.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
        // Validamos que exista el usuario y que el refresh token no haya expirado
        if (usuario == null)
        {
            throw new ValidationException("Refresh token inválido.");
        }
        // Validamos que el refresh token no haya expirado
        if (usuario.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new ValidationException("Refresh token expirado.");
        }
        // Generamos nuevos tokens
        var newAccessToken = tokenService.CrearToken(usuario);
        var newRefreshToken = tokenService.GenerarRefreshToken();
        // Actualizamos el usuario con el nuevo refresh token (Rotación de tokens)
        usuario.SetRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(30));
        await context.SaveChangesAsync();
        // Devolvemos los nuevos tokens
        return new TokenResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        };
    }

    /// <summary>
    /// Verifica el email de un usuario usando el token de verificación.
    /// </summary>
    /// <param name="token">El token enviado al email del usuario.</param>
    /// <returns>Un AuthResult indicando el éxito o fracaso.</returns>
    public async Task<AuthResult> VerifyEmailAsync(string token)
    {
        // Decodifica el token que viene de la URL
        var decodedToken = WebUtility.UrlDecode(token);
        // Busca en la BD usando el token decodificado
        var usuario = await context.Usuarios.FirstOrDefaultAsync(u => u.EmailVerificationToken == token);
        // Valida que exista el usuario y que el token no haya expirado
        if (usuario == null)
        {
            return AuthResult.Fail("Token de verificación inválido.");
        }
        // Marca el email del usuario como verificado
        usuario.MarkEmailAsVerified();
        // Guarda los cambios en la base de datos
        await context.SaveChangesAsync();
        // Devuelve un resultado exitoso.
        return AuthResult.Ok(null, "Email verificado exitosamente.");
    }

    /// <summary>
    /// Inicia el proceso de reseteo de contraseña para un email.
    /// </summary>
    public async Task<AuthResult> ForgotPasswordAsync(string email)
    {
        // 1. Buscar al usuario por su email
        var usuario = await context.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        // 2. ¡IMPORTANTE! Por seguridad, NUNCA reveles si el email existe o no.
        //    Devuelve siempre un mensaje de éxito genérico.
        if (usuario != null)
        {
            // 3. Generar un token de reseteo
            var resetToken = tokenService.GenerarRefreshToken();

            // 4. Establecer el token y una hora de expiración (ej. 1 hora)
            usuario.SetPasswordResetToken(resetToken, DateTime.UtcNow.AddHours(1));

            // 5. Guardar el token en la base de datos
            await context.SaveChangesAsync();

            // 6. Enviar el email de reseteo
            await SendPasswordResetEmailAsync(usuario);
        }

        // 7. Devolver siempre este mensaje
        return AuthResult.Ok(null, "Si existe una cuenta con ese correo, se ha enviado un enlace para restablecer la contraseña.");
    }

    /// <summary>
    /// Completa el proceso de reseteo de contraseña usando un token.
    /// </summary>
    public async Task<AuthResult> ResetPasswordAsync(ResetPasswordDto dto)
    {
        // Decodifica el token que viene del DTO (que vino de la URL)
        var decodedToken = WebUtility.UrlDecode(dto.Token);
        // Busca en la BD usando el token decodificado
        var usuario = await context.Usuarios.FirstOrDefaultAsync(u => u.PasswordResetToken == decodedToken);

        // 2. Validar el token y su expiración
        if (usuario == null)
        {
            return AuthResult.Fail("El token de restablecimiento no es válido.");
        }

        if (usuario.PasswordResetTokenExpiryTime <= DateTime.UtcNow)
        {
            return AuthResult.Fail("El token de restablecimiento ha expirado.");
        }

        // 3. Hashear y establecer la nueva contraseña
        var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        usuario.EstablecerPasswordHash(newPasswordHash);

        // 4. Limpiar el token para que no se pueda reusar
        usuario.ClearPasswordResetToken();

        // 5. Guardar los cambios
        await context.SaveChangesAsync();

        return AuthResult.Ok(null, "Contraseña restablecida exitosamente.");
    }
    
    #region Private Methods

    /// <summary>
    /// Método helper privado para construir y enviar el email de reseteo.
    /// </summary>
    private async Task SendPasswordResetEmailAsync(Usuario usuario)
    {
        // 1. Esta lógica (crear el enlace) sigue siendo la misma
        var frontendBaseUrl = configuration["AppSettings:FrontendBaseUrl"];
        var encodedToken = WebUtility.UrlEncode(usuario.PasswordResetToken);
        var resetLink = $"{frontendBaseUrl}/reset-password?token={encodedToken}";

        var emailSubject = "Restablece tu contraseña de AccountPanel";

        // 2. Lee la plantilla HTML desde el archivo
        var templatePath = Path.Combine(AppContext.BaseDirectory, "EmailTemplates", "PasswordResetEmail.html");
        var emailBody = await File.ReadAllTextAsync(templatePath);

        // 3. Reemplaza los placeholders con los datos reales
        emailBody = emailBody.Replace("{{UserName}}", usuario.NombreCompleto);
        emailBody = emailBody.Replace("{{Link}}", resetLink);
        // 4. La lógica de envío (try-catch) sigue siendo la misma
        try
        {
            await emailService.SendEmailAsync(usuario.Email, emailSubject, emailBody);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al enviar email de reseteo: {ex.Message}");
        }
    }

    /// <summary>
    /// Construye y envía el correo de verificación de email.
    /// </summary>
    /// <param name="usuario">El usuario al que se le enviará el correo.</param>
    private async Task SendVerificationEmailAsync(Usuario usuario)
    {
        // 1. Construir el enlace de verificación
        var frontendBaseUrl = configuration["AppSettings:FrontendBaseUrl"];
        // ¡Importante! Usamos el token que ya está guardado en el usuario
        var encodedToken = WebUtility.UrlEncode(usuario.EmailVerificationToken);
        var verificationLink = $"{frontendBaseUrl}/verify-email?token={encodedToken}";
        // 2. Leer el contenido del email
        var templatePath = Path.Combine(AppContext.BaseDirectory, "EmailTemplates", "VerificationEmail.html");
        var emailBody = await File.ReadAllTextAsync(templatePath);

        // 3. Reemplazar los placeholders
        emailBody = emailBody.Replace("{{UserName}}", usuario.NombreCompleto);
        emailBody = emailBody.Replace("{{Link}}", verificationLink);

        var emailSubject = "¡Bienvenido a AccountPanel! Confirma tu email";

        // 4. Enviar el email (con el try-catch)
        try
        {
            await emailService.SendEmailAsync(usuario.Email, emailSubject, emailBody);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al enviar email de verificación: {ex.Message}");
        }
    }
    
    #endregion
}