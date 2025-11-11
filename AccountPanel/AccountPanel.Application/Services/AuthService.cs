using AccountPanel.Application.DTOs;
using AccountPanel.Application.Exceptions;
using AccountPanel.Application.Interfaces;
using AccountPanel.Domain.Models;
using Microsoft.EntityFrameworkCore;

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
    IExternalAuthValidator externalAuthValidator) : IAuthService
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
}