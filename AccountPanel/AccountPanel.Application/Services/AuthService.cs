using AccountPanel.Application.DTOs;
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
        var nuevoUsuario = new Usuario(registroDto.NombreCompleto, registroDto.Email, registroDto.NumeroTelefono,
            RolUsuario.User);
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
    public async Task<AuthResult> LoginAsync(LoginUsuarioDto loginDto)
    {
        // Busca al usuario por su email a través del contrato del contexto.
        var usuario = await context.Usuarios.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

        // Valida si el usuario existe y si la contraseña proporcionada es correcta.
        if (usuario == null || usuario.PasswordHash == null ||
            !BCrypt.Net.BCrypt.Verify(loginDto.Password, usuario.PasswordHash))
        {
            return AuthResult.Fail("Credenciales inválidas.");
        }

        // Delega la creación del token al servicio correspondiente a través de la interfaz ITokenService.
        var token = tokenService.CrearToken(usuario);
        return AuthResult.Ok(token);
    }

    /// <summary>
    /// Autentica a un usuario utilizando un token de un proveedor externo (ej. Google).
    /// </summary>
    /// <param name="externalLoginDto">DTO con el nombre del proveedor y el token de ID.</param>
    /// <returns>Un AuthResult que contiene el token JWT si la autenticación es exitosa.</returns>
    public async Task<AuthResult> ExternalLoginAsync(ExternalLoginDto externalLoginDto)
    {
        if (externalLoginDto.Provider.ToLower() != "google")
        {
            return AuthResult.Fail("Proveedor no soportado.");
        }

        // 1. Delega la validación técnica del token a un servicio de infraestructura a través de la interfaz.
        // La capa de aplicación no sabe que se está comunicando con Google; solo sabe que valida un token.
        var userInfo = await externalAuthValidator.ValidateTokenAsync(externalLoginDto.IdToken);
        if (userInfo == null)
        {
            return AuthResult.Fail("Token externo inválido.");
        }

        // 2. Comienza la lógica de negocio: buscar si el login externo ya existe.
        var userLogin = await context.UserLogins.Include(ul => ul.Usuario)
            .FirstOrDefaultAsync(ul => ul.LoginProvider == "Google" && ul.ProviderKey == userInfo.ProviderSubjectId);

        // Si el usuario ya se ha logueado antes con este proveedor, simplemente genera un nuevo token.
        if (userLogin != null)
        {
            var existingToken = tokenService.CrearToken(userLogin.Usuario);
            return AuthResult.Ok(existingToken);
        }

        // 3. Si es un nuevo login, buscar si ya existe un usuario local con ese email.
        var usuario = await context.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == userInfo.Email.ToLower());
        if (usuario == null)
        {
            // Si el usuario es completamente nuevo en el sistema, se crea un registro para él.
            usuario = new Usuario(userInfo.Name, userInfo.Email, "", RolUsuario.User);
            await context.Usuarios.AddAsync(usuario);
            await context.SaveChangesAsync();
        }

        // 4. Se crea el registro del login externo y se asocia con la cuenta de usuario (nueva o existente).
        var nuevoLogin = new UserLogin("Google", userInfo.ProviderSubjectId, usuario);
        await context.UserLogins.AddAsync(nuevoLogin);
        await context.SaveChangesAsync();

        // 5. Se genera y devuelve el token JWT para la sesión del usuario.
        var newToken = tokenService.CrearToken(usuario);
        return AuthResult.Ok(newToken);
    }
}