using System.Net;
using System.Net.Http.Json;
using AccountPanel.Application.DTOs;
using AccountPanel.Domain.Models;
using AccountPanel.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AccountPanel.Api.IntegrationTests;

/// <summary>
/// Contiene las pruebas de integración para el AuthController.
/// Estas pruebas verifican el comportamiento de los endpoints de registro y login
/// interactuando con una versión en memoria de la API y su base de datos.
/// </summary>
public class AuthControllerTests : IClassFixture<TestApiFactory>, IAsyncLifetime
{
    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;
    private const string ApiVersion = "v1";

    /// <summary>
    /// El constructor recibe la instancia de la fábrica de API (inyectada por xUnit)
    /// y la utiliza para crear un cliente HTTP que puede enviar peticiones a la API en memoria.
    /// </summary>
    public AuthControllerTests(TestApiFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    /// <summary>
    /// Este método se ejecuta ANTES de cada test en esta clase.
    /// Llama al método de reseteo de la fábrica para asegurar que cada test
    /// comience con una base de datos completamente limpia.
    /// </summary>
    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();

    /// <summary>
    /// Este método se ejecuta DESPUÉS de cada test. Por ahora, no se necesita limpieza adicional.
    /// </summary>
    public Task DisposeAsync() => Task.CompletedTask;

    #region Register Testing

    /// <summary>
    /// Prueba el "camino feliz": un registro exitoso con datos válidos.
    /// </summary>
    [Fact]
    public async Task Register_WithValidData_ShouldReturnSuccessAndCreateUser()
    {
        // --- Arrange (Preparar) ---
        // Se crea un DTO con datos de registro válidos.
        var registroDto = new RegistroUsuarioDto
        {
            NombreCompleto = "Usuario de Prueba",
            Email = "test@email.com",
            Password = "PasswordValida123",
            NumeroTelefono = "1234567890"
        };

        // --- Act (Actuar) ---
        // Se envía una petición POST al endpoint de registro.
        var response = await _client.PostAsJsonAsync(requestUri: $"/api/{ApiVersion}/auth/register",
            value: registroDto);

        // --- Assert (Verificar) ---
        // 1. Se verifica que la respuesta HTTP sea 200 OK.
        response.StatusCode.Should().Be(expected: HttpStatusCode.OK);
        var responseBody = await response.Content.ReadFromJsonAsync<object>();
        responseBody.Should().NotBeNull();

        // 2. Se verifica directamente en la base de datos que el usuario fue creado correctamente.
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var newUser = await context.Usuarios.FirstOrDefaultAsync(predicate: u => u.Email == registroDto.Email);
        newUser.Should().NotBeNull();
        newUser.NombreCompleto.Should().Be(expected: registroDto.NombreCompleto);
    }

    /// <summary>
    /// Prueba que la API rechace un intento de registro con un email que ya existe.
    /// </summary>
    [Fact]
    public async Task Register_WithExistingEmail_ShouldReturnBadRequest()
    {
        // --- Arrange (Preparar) ---
        // 1. Se pre-carga la base de datos con un usuario para simular que el email ya está en uso.
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var initialUser = new Usuario(
                nombreCompleto: "Usuario Original",
                email: "existente@email.com",
                numeroTelefono: "123456789",
                rol: RolUsuario.User);
            initialUser.EstablecerPasswordHash(passwordHash: BCrypt.Net.BCrypt.HashPassword(inputKey: "password123"));
            await context.Usuarios.AddAsync(entity: initialUser);
            await context.SaveChangesAsync();
        }

        // 2. Se crea un DTO que intenta usar el mismo email.
        var registroDto = new RegistroUsuarioDto
        {
            NombreCompleto = "Usuario Duplicado",
            Email = "existente@email.com",
            Password = "password456",
            NumeroTelefono = "987654321"
        };

        // --- Act (Actuar) ---
        // Se envía la petición de registro.
        var response = await _client.PostAsJsonAsync(requestUri: $"/api/{ApiVersion}/auth/register",
            value: registroDto);

        // --- Assert (Verificar) ---
        // Se verifica que la API devuelva un error 400 Bad Request.
        response.StatusCode.Should().Be(expected: HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Prueba múltiples casos de datos de entrada inválidos usando la teoría de xUnit.
    /// Esto verifica que las reglas de FluentValidation están funcionando correctamente.
    /// </summary>
    [Theory]
    [InlineData(data: ["", "valido@email.com", "passvalido", "Teléfono Válido", "El nombre es obligatorio."])]
    [InlineData(data: ["Nombre Válido", "email-invalido", "passvalido", "Teléfono Válido", "El formato del email no es válido."])]
    [InlineData(data: ["Nombre Válido", "valido@email.com", "corta", "Teléfono Válido", "La contraseña debe tener al menos 8 caracteres."])]
    public async Task Register_WithInvalidData_ShouldReturnBadRequest(string nombre, string email, string password,
        string telefono, string expectedError)
    {
        // --- Arrange (Preparar) ---
        // Se crea un DTO con los datos de prueba inválidos proporcionados por [InlineData].
        var registroDto = new RegistroUsuarioDto
        {
            NombreCompleto = nombre,
            Email = email,
            Password = password,
            NumeroTelefono = telefono
        };

        // --- Act (Actuar) ---
        // Se envía la petición.
        var response = await _client.PostAsJsonAsync(requestUri: $"/api/{ApiVersion}/auth/register",
            value: registroDto);

        // --- Assert (Verificar) ---
        // 1. Se verifica que el código de estado sea 400 Bad Request.
        response.StatusCode.Should().Be(expected: HttpStatusCode.BadRequest);

        // 2. Se verifica que el cuerpo de la respuesta contenga el mensaje de error específico que esperamos de FluentValidation.
        var errorBody = await response.Content.ReadAsStringAsync();
        errorBody.Should().Contain(expected: expectedError);
    }

    #endregion

    #region Login Testing

    /// <summary>
    /// Prueba el "camino feliz" del login: un usuario con credenciales correctas.
    /// </summary>
    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnOkAndJwtToken()
    {
        // --- Arrange (Preparar) ---
        var password = "PasswordSegura123";
        var email = "login.test@email.com";
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = new Usuario(nombreCompleto: "Test Login",
                email: email,
                numeroTelefono: "111222333",
                rol: RolUsuario.User);
            user.EstablecerPasswordHash(passwordHash: BCrypt.Net.BCrypt.HashPassword(inputKey: password));
            await context.Usuarios.AddAsync(entity: user);
            await context.SaveChangesAsync();
        }

        var loginDto = new LoginUsuarioDto { Email = email, Password = password };

        // --- Act (Actuar) ---
        var response = await _client.PostAsJsonAsync(requestUri: $"/api/{ApiVersion}/auth/login",
            value: loginDto);

        // --- Assert (Verificar) ---
        response.StatusCode.Should().Be(expected: HttpStatusCode.OK);

        // 1. Verificamos que el cuerpo de la respuesta sea el DTO esperado.
        var responseData = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
        responseData.Should().NotBeNull();
        responseData.AccessToken.Should().NotBeNullOrEmpty();
        responseData.RefreshToken.Should().NotBeNullOrEmpty();

        // 2. Verificamos que el refresh token se guardó en la base de datos.
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
            user.Should().NotBeNull();
            user.RefreshToken.Should().Be(responseData.RefreshToken);
            user.RefreshTokenExpiryTime.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), precision: TimeSpan.FromSeconds(10));
        }
    }

    #endregion
}