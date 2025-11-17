using AccountPanel.Application.DTOs;
using AccountPanel.Application.Interfaces;
using AccountPanel.Application.Services;
using AccountPanel.Domain.Models;
using FluentAssertions;
using Moq;
using Moq.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AccountPanel.Application.UnitTests;

/// <summary>
/// Contiene las pruebas unitarias para la clase AuthService.
/// El objetivo es verificar la lógica de negocio de AuthService de forma aislada.
/// Para lograr este aislamiento, se simulan ("mockean") todas sus dependencias externas
/// utilizando las interfaces (contratos) que consume, como IApplicationDbContext o ITokenService.
/// Esto garantiza que probamos únicamente la lógica de AuthService, no la base de datos ni otros servicios.
/// </summary>
public class AuthServiceTests
{
    // Mocks para los contratos (interfaces) que AuthService necesita.
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<IExternalAuthValidator> _mockExternalAuthValidator;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IConfiguration> _mockConfiguration;

    // La instancia real del servicio que vamos a probar, inyectado con nuestras dependencias simuladas.
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        // --- Arrange Global (Preparación que se ejecuta antes de cada prueba) ---

        _mockDbContext = new Mock<IApplicationDbContext>();
        _mockTokenService = new Mock<ITokenService>();
        _mockExternalAuthValidator = new Mock<IExternalAuthValidator>();
        _mockEmailService = new Mock<IEmailService>();
        _mockConfiguration = new Mock<IConfiguration>();

        // 4. Se crea la instancia del AuthService, pasándole los objetos simulados (.Object).
        _authService = new AuthService(
            context: _mockDbContext.Object,
            tokenService: _mockTokenService.Object,
            externalAuthValidator: _mockExternalAuthValidator.Object,
            emailService: _mockEmailService.Object, 
            configuration: _mockConfiguration.Object
        );
        // Simula la configuración de appsettings.json
        var mockConfigSection = new Mock<IConfigurationSection>();
        mockConfigSection.Setup(s => s.Value).Returns("http://localhost:5173");
        _mockConfiguration.Setup(c => c.GetSection("AppSettings:FrontendBaseUrl"))
                          .Returns(mockConfigSection.Object);
    }

    /// <summary>
    /// Prueba el "camino feliz" del registro: un usuario con un email nuevo debería poder registrarse.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_WithNewEmail_ShouldReturnSuccess()
    {
        // --- Arrange (Preparar) ---
        var registroDto = new RegistroUsuarioDto
        {
            Email = "nuevo@email.com",
            Password = "password123",
            NombreCompleto = "Nuevo Usuario",
            NumeroTelefono = "123456"
        };

        // Se configura el mock del DbContext para que devuelva una lista vacía
        // al consultar los usuarios. Esto simula que el email no está en uso.
        _mockDbContext.Setup(x => x.Usuarios).ReturnsDbSet(new List<Usuario>());

        // --- Act (Actuar) ---
        // Se invoca el método que se está probando.
        var result = await _authService.RegisterAsync(registroDto);

        // --- Assert (Verificar) ---
        // Se comprueba que el resultado de la operación sea exitoso.
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Usuario registrado exitosamente.");

        // Se verifica que el método para guardar cambios en el contexto fue llamado
        // exactamente una vez. Esto confirma que el servicio intentó persistir los datos.
        _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Prueba el caso de error: un intento de registro con un email que ya existe debería fallar.
    /// </summary>
    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldReturnFail()
    {
        // --- Arrange (Preparar) ---
        var registroDto = new RegistroUsuarioDto { Email = "existente@email.com", Password = "password123" };
        var existingUser = new List<Usuario>
            { new("Usuario Existente", "existente@email.com", "123", RolUsuario.User) };

        // Se configura el mock para que, al consultar los usuarios, devuelva una lista
        // que contiene un usuario con el mismo email, simulando que ya está en uso.
        _mockDbContext.Setup(x => x.Usuarios).ReturnsDbSet(existingUser);

        // --- Act (Actuar) ---
        var result = await _authService.RegisterAsync(registroDto);

        // --- Assert (Verificar) ---
        // Se comprueba que el resultado sea un fallo con el mensaje de error esperado.
        result.Success.Should().BeFalse();
        result.Message.Should().Be("El correo electrónico ya está en uso.");
    }

    /// <summary>
    /// Prueba el "camino feliz" del login externo: un usuario nuevo que se autentica por primera vez.
    /// </summary>
    [Fact]
    public async Task ExternalLoginAsync_WithNewUser_ShouldCreateUserAndReturnToken()
    {
        // --- Arrange (Preparar) ---
        var externalLoginDto = new ExternalLoginDto { Provider = "Google", IdToken = "valid-google-token" };
        var userInfo = new ExternalAuthUserInfo
        {
            Email = "google.user@email.com",
            Name = "Google User",
            ProviderSubjectId = "google-user-id-123"
        };

        // 1. Se configura el validador externo para que devuelva un usuario válido.
        // Esto simula una validación de token de Google exitosa.
        _mockExternalAuthValidator.Setup(v => v.ValidateTokenAsync(externalLoginDto.IdToken))
            .ReturnsAsync(userInfo);

        // 2. Se configura el contexto para que no encuentre ni logins ni usuarios existentes.
        _mockDbContext.Setup(c => c.UserLogins).ReturnsDbSet(new List<UserLogin>());
        _mockDbContext.Setup(c => c.Usuarios).ReturnsDbSet(new List<Usuario>());

        // 3. Se configura el servicio de token para que devuelva un token de prueba.
        _mockTokenService.Setup(t => t.CrearToken(It.IsAny<Usuario>())).Returns("jwt-access-token");
        _mockTokenService.Setup(t => t.GenerarRefreshToken()).Returns("jwt-refresh-token");
        // --- Act (Actuar) ---
        var result = await _authService.ExternalLoginAsync(externalLoginDto);

        // --- Assert (Verificar) ---
        // Se comprueba que la operación fue exitosa y devolvió el token esperado.
        result.Should().NotBeNull();
        result.Should().BeOfType<TokenResponseDto>();
        result.AccessToken.Should().Be("jwt-access-token");
        result.RefreshToken.Should().Be("jwt-refresh-token");

        // Se verifica que se intentó guardar el nuevo usuario y el nuevo login externo.
        _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task VerifyEmailAsync_WithValidToken_ShouldVerifyUser()
    {
        // --- Arrange (Preparar) ---
        var fakeToken = "valid-token";
        var usuario = new Usuario("Test", "test@test.com", "123", RolUsuario.User);
        usuario.SetEmailVerificationToken(fakeToken); // El usuario tiene el token
        
        // Creamos una lista que contiene al usuario
        var usersList = new List<Usuario> { usuario };

        // Simula la colección Usuarios completa usando Moq.EntityFrameworkCore
        _mockDbContext.Setup(c => c.Usuarios).ReturnsDbSet(usersList);

        // --- Act (Actuar) ---
        var result = await _authService.VerifyEmailAsync(fakeToken);

        // --- Assert (Verificar) ---
        result.Success.Should().BeTrue();
        
        // Verifica que el usuario fue modificado en memoria
        usuario.IsEmailVerified.Should().BeTrue();
        usuario.EmailVerificationToken.Should().BeNull();
        
        // Verifica que se guardaron los cambios
        _mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyEmailAsync_WithInvalidToken_ShouldReturnFail()
    {
        // --- Arrange (Preparar) ---
        var fakeToken = "invalid-token";
        
        // Creamos una lista VACÍA
        var usersList = new List<Usuario>();

        // Simula la colección Usuarios completa (vacía)
        _mockDbContext.Setup(c => c.Usuarios).ReturnsDbSet(usersList);

        // --- Act (Actuar) ---
        var result = await _authService.VerifyEmailAsync(fakeToken);

        // --- Assert (Verificar) ---
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Token de verificación inválido.");
        
        // Verifica que NO se guardaron cambios
        _mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #region ForgotPasswordAsync Tests

    [Fact]
    public async Task ForgotPasswordAsync_WhenUserExists_ShouldSetTokenAndSendEmail()
    {
        // --- Arrange (Preparar) ---
        var userEmail = "test@test.com";
        var fakeToken = "fake-reset-token-123";
        var usuario = new Usuario("Test User", userEmail, "123", RolUsuario.User);
        var usersList = new List<Usuario> { usuario };

        // 1. Simula la colección Usuarios
        _mockDbContext.Setup(c => c.Usuarios).ReturnsDbSet(usersList);

        // 2. Simula el generador de tokens
        _mockTokenService.Setup(s => s.GenerarRefreshToken()).Returns(fakeToken);

        // --- Act (Actuar) ---
        var result = await _authService.ForgotPasswordAsync(userEmail);

        // --- Assert (Verificar) ---
        // 1. Verifica que el resultado fue el mensaje genérico
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Si existe una cuenta con ese correo, se ha enviado un enlace para restablecer la contraseña.");

        // 2. Verifica que el token se guardó en el usuario
        usuario.PasswordResetToken.Should().Be(fakeToken);
        usuario.PasswordResetTokenExpiryTime.Should().BeCloseTo(DateTime.UtcNow.AddHours(1), precision: TimeSpan.FromSeconds(5));

        // 3. Verifica que se guardó en la BD y se envió el email
        _mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockEmailService.Verify(s => s.SendEmailAsync(
            It.Is<string>(email => email == userEmail),
            It.IsAny<string>(), // subject
            It.Is<string>(body => body.Contains(fakeToken)) // body
        ), Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WhenUserDoesNotExist_ShouldReturnSuccessSilently()
    {
        // --- Arrange (Preparar) ---
        var userEmail = "no-existe@test.com";
        var usersList = new List<Usuario>(); // Lista vacía

        _mockDbContext.Setup(c => c.Usuarios).ReturnsDbSet(usersList);

        // --- Act (Actuar) ---
        var result = await _authService.ForgotPasswordAsync(userEmail);

        // --- Assert (Verificar) ---
        // 1. Verifica que el resultado es el mismo mensaje (por seguridad)
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Si existe una cuenta con ese correo, se ha enviado un enlace para restablecer la contraseña.");

        // 2. Verifica que NO se guardó nada y NO se envió email
        _mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockEmailService.Verify(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region ResetPasswordAsync Tests

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ShouldResetPassword()
    {
        // --- Arrange (Preparar) ---
        var fakeToken = "valid-token-456";
        var dto = new ResetPasswordDto { Token = fakeToken, NewPassword = "NewPassword123!", ConfirmPassword = "NewPassword123!" };
        var oldHash = BCrypt.Net.BCrypt.HashPassword("OldPassword");

        var usuario = new Usuario("Test User", "test@test.com", "123", RolUsuario.User);
        usuario.EstablecerPasswordHash(oldHash);
        // El token es válido y expira en el futuro
        usuario.SetPasswordResetToken(fakeToken, DateTime.UtcNow.AddHours(1));

        var usersList = new List<Usuario> { usuario };
        _mockDbContext.Setup(c => c.Usuarios).ReturnsDbSet(usersList);

        // --- Act (Actuar) ---
        var result = await _authService.ResetPasswordAsync(dto);

        // --- Assert (Verificar) ---
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Contraseña restablecida exitosamente.");

        // 1. Verifica que la contraseña cambió
        usuario.PasswordHash.Should().NotBe(oldHash);

        // 2. Verifica que el token fue limpiado
        usuario.PasswordResetToken.Should().BeNull();
        usuario.PasswordResetTokenExpiryTime.Should().BeNull();

        // 3. Verifica que se guardaron los cambios
        _mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithInvalidToken_ShouldReturnFail()
    {
        // --- Arrange (Preparar) ---
        var dto = new ResetPasswordDto { Token = "invalid-token", /* ... */ };
        _mockDbContext.Setup(c => c.Usuarios).ReturnsDbSet(new List<Usuario>()); // No se encuentra el token

        // --- Act (Actuar) ---
        var result = await _authService.ResetPasswordAsync(dto);

        // --- Assert (Verificar) ---
        result.Success.Should().BeFalse();
        result.Message.Should().Be("El token de restablecimiento no es válido.");
        _mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithExpiredToken_ShouldReturnFail()
    {
        // --- Arrange (Preparar) ---
        var fakeToken = "expired-token";
        var dto = new ResetPasswordDto { Token = fakeToken, /* ... */ };

        var usuario = new Usuario("Test User", "test@test.com", "123", RolUsuario.User);
        // El token expiró en el pasado
        usuario.SetPasswordResetToken(fakeToken, DateTime.UtcNow.AddHours(-1));

        var usersList = new List<Usuario> { usuario };
        _mockDbContext.Setup(c => c.Usuarios).ReturnsDbSet(usersList);

        // --- Act (Actuar) ---
        var result = await _authService.ResetPasswordAsync(dto);

        // --- Assert (Verificar) ---
        result.Success.Should().BeFalse();
        result.Message.Should().Be("El token de restablecimiento ha expirado.");
        _mockDbContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}