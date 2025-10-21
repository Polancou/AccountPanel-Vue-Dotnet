using BaseApp.Application.DTOs;
using BaseApp.Application.Interfaces;
using BaseApp.Application.Services;
using BaseApp.Domain.Models;
using FluentAssertions;
using Moq;
using Moq.EntityFrameworkCore;

namespace BaseApp.Application.UnitTests;

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

    // La instancia real del servicio que vamos a probar, inyectado con nuestras dependencias simuladas.
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        // --- Arrange Global (Preparación que se ejecuta antes de cada prueba) ---

        // 1. Se crea un mock para el contrato del contexto de la base de datos.
        _mockDbContext = new Mock<IApplicationDbContext>();

        // 2. Se crea un mock para el contrato del servicio de tokens.
        _mockTokenService = new Mock<ITokenService>();

        // 3. Se crea un mock para el contrato del validador de tokens externos.
        _mockExternalAuthValidator = new Mock<IExternalAuthValidator>();

        // 4. Se crea la instancia del AuthService, pasándole los objetos simulados (.Object).
        _authService = new AuthService(
            context: _mockDbContext.Object,
            tokenService: _mockTokenService.Object,
            externalAuthValidator: _mockExternalAuthValidator.Object
        );
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
        _mockTokenService.Setup(t => t.CrearToken(It.IsAny<Usuario>())).Returns("jwt-test-token");

        // --- Act (Actuar) ---
        var result = await _authService.ExternalLoginAsync(externalLoginDto);

        // --- Assert (Verificar) ---
        // Se comprueba que la operación fue exitosa y devolvió el token esperado.
        result.Success.Should().BeTrue();
        result.Token.Should().Be("jwt-test-token");

        // Se verifica que se intentó guardar el nuevo usuario y el nuevo login externo.
        _mockDbContext.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}