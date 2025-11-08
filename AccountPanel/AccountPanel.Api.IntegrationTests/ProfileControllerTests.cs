using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AccountPanel.Application.DTOs;
using AccountPanel.Infrastructure.Data;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace AccountPanel.Api.IntegrationTests;

/// <summary>
/// Contiene las pruebas de integración para el ProfileController.
/// Estas pruebas verifican que los endpoints protegidos se comporten como se espera,
/// devolviendo datos del perfil con un token válido y denegando el acceso sin uno.
/// </summary>
public class ProfileControllerTests : IClassFixture<TestApiFactory>, IAsyncLifetime
{
    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;
    private const string ApiVersion = "v1";

    /// <summary>
    /// El constructor recibe la instancia de la fábrica de API (inyectada por xUnit)
    /// y la utiliza para crear un cliente HTTP que puede enviar peticiones a la API en memoria.
    /// </summary>
    public ProfileControllerTests(TestApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    /// <summary>
    /// Se ejecuta ANTES de cada test. Limpia la base de datos para asegurar
    /// que cada prueba se ejecute en un estado aislado y predecible.
    /// </summary>
    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();

    /// <summary>
    /// Se ejecuta DESPUÉS de cada test. No se requiere limpieza adicional en este caso.
    /// </summary>
    public Task DisposeAsync() => Task.CompletedTask;

    #region Pruebas para obtener Perfil de usuario

    /// <summary>
    /// Prueba el "camino feliz": un usuario autenticado con un token válido
    /// debería poder acceder a su propio perfil.
    /// </summary>
    [Fact]
    public async Task GetMyProfile_WithValidToken_ShouldReturnUserProfile()
    {
        // --- Arrange (Preparar) ---
        var (userId, token) = await _factory.CreateUserAndGetTokenAsync(
            name: "Usuario Original",
            email: "test@email.com");

        // 3. Se añade el token JWT al encabezado 'Authorization' de la petición HTTP.
        // El esquema "Bearer" es el estándar para los tokens JWT.
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme: "Bearer",
            parameter: token);

        // --- Act (Actuar) ---
        // Se envía una petición GET al endpoint protegido '/api/profile/me'.
        var response = await _client.GetAsync(requestUri: $"/api/{ApiVersion}/profile/me");

        // --- Assert (Verificar) ---
        // 1. Se verifica que la respuesta HTTP sea 200 OK.
        response.StatusCode.Should().Be(expected: HttpStatusCode.OK);

        // 2. Se deserializa el cuerpo de la respuesta JSON a un DTO de perfil.
        var profile = await response.Content.ReadFromJsonAsync<PerfilUsuarioDto>();

        // 3. Se verifica que los datos del perfil devuelto coincidan con los del usuario creado.
        profile.Should().NotBeNull();
        profile.Id.Should().Be(expected: userId);
        profile.Email.Should().Be(expected: "test@email.com");
        profile.NombreCompleto.Should().Be(expected: "Usuario Original");
    }

    /// <summary>
    /// Prueba que un intento de acceso a un endpoint protegido sin un token JWT
    /// sea rechazado con un estado de No Autorizado.
    /// </summary>
    [Fact]
    public async Task GetMyProfile_WithoutToken_ShouldReturnUnauthorized()
    {
        // --- Arrange (Preparar) ---
        // Se asegura de que no haya ningún token en el encabezado de autorización.
        _client.DefaultRequestHeaders.Authorization = null;

        // --- Act (Actuar) ---
        // Se envía la petición GET al endpoint protegido.
        var response = await _client.GetAsync(requestUri: $"/api/{ApiVersion}/profile/me");

        // --- Assert (Verificar) ---
        // Se verifica que la respuesta HTTP sea 401 Unauthorized, como se espera.
        response.StatusCode.Should().Be(expected: HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Pruebas de Actualización de Perfil (Update)

    /// <summary>
    /// Prueba el "camino feliz": un usuario autenticado con un token válido
    /// debería poder actualizar su propio perfil.
    /// </summary>
    [Fact]
    public async Task UpdateMyProfile_WithValidTokenAndData_ShouldUpdateUserAndReturnNoContent()
    {
        // --- Arrange (Preparar) ---
        var (userId, token) = await _factory.CreateUserAndGetTokenAsync(
            name: "Usuario Original",
            email: "update.test@email.com");

        // 1. Se añade el token JWT al encabezado 'Authorization' de la petición.
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme: "Bearer",
            parameter: token);

        // 2. Se crea el DTO con los nuevos datos del perfil.
        var updateDto = new ActualizarPerfilDto
        {
            NombreCompleto = "Usuario Actualizado",
            NumeroTelefono = "9876543210"
        };

        // --- Act (Actuar) ---
        // Se envía una petición PUT al endpoint protegido '/api/v1/profile/me' con los nuevos datos.
        var response = await _client.PutAsJsonAsync(requestUri: $"/api/{ApiVersion}/profile/me",
            value: updateDto);

        // --- Assert (Verificar) ---
        // 1. Se verifica que la respuesta HTTP sea 204 No Content, el estándar para una actualización exitosa.
        response.StatusCode.Should().Be(expected: HttpStatusCode.NoContent);

        // 2. Se verifica directamente en la base de datos que los datos del usuario fueron actualizados.
        // Esto confirma que todo el flujo (Controller -> Service -> DbContext) funcionó correctamente.
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updatedUser = await context.Usuarios.FindAsync(keyValues: userId);

        updatedUser.Should().NotBeNull();
        updatedUser.NombreCompleto.Should().Be(expected: updateDto.NombreCompleto);
        updatedUser.NumeroTelefono.Should().Be(expected: updateDto.NumeroTelefono);
    }

    /// <summary>
    /// Prueba que un intento de actualizar el perfil sin un token JWT
    /// sea rechazado con un estado de No Autorizado (401).
    /// </summary>
    [Fact]
    public async Task UpdateMyProfile_WithoutToken_ShouldReturnUnauthorized()
    {
        // --- Arrange (Preparar) ---
        // Se asegura de que no haya ningún token en el encabezado de autorización.
        _client.DefaultRequestHeaders.Authorization = null;
        var updateDto = new ActualizarPerfilDto { NombreCompleto = "No importa", NumeroTelefono = "123" };

        // --- Act (Actuar) ---
        // Se envía la petición PUT al endpoint protegido.
        var response = await _client.PutAsJsonAsync(requestUri: $"/api/{ApiVersion}/profile/me",
            value: updateDto);

        // --- Assert (Verificar) ---
        // Se verifica que la respuesta HTTP sea 401 Unauthorized.
        response.StatusCode.Should().Be(expected: HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Pruebas de Cambio de Contraseña

    /// <summary>
    /// Prueba el "camino feliz": un usuario autenticado con la contraseña antigua correcta
    /// debería poder actualizar su contraseña y recibir un 200 OK.
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithValidData_ShouldReturnOk()
    {
        // --- Arrange (Preparar) ---
        // El helper CreateUserAndGetTokenAsync usa "password123" como contraseña por defecto
        var (userId, token) = await _factory.CreateUserAndGetTokenAsync(name: "User", email: "pass.test@email.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme: "Bearer", parameter: token);

        var dto = new CambiarPasswordDto
        {
            OldPassword = "password123", // Contraseña correcta del helper
            NewPassword = "P@ssw0rdNueva456!",
            ConfirmPassword = "P@ssw0rdNueva456!"
        };

        // --- Act (Actuar) ---
        var response = await _client.PutAsJsonAsync($"/api/{ApiVersion}/profile/change-password", dto);

        // --- Assert (Verificar) ---
        // 1. Verifica la respuesta HTTP
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        result.Should().NotBeNull();
        result["message"].Should().Be("Contraseña actualizada exitosamente.");

        // 2. Verifica que la contraseña realmente cambió en la base de datos
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updatedUser = await context.Usuarios.FindAsync(userId);
        BCrypt.Net.BCrypt.Verify(dto.NewPassword, updatedUser.PasswordHash).Should().BeTrue();
    }

    /// <summary>
    /// Prueba que el endpoint devuelva 400 Bad Request si la contraseña antigua es incorrecta.
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithWrongOldPassword_ShouldReturnBadRequest()
    {
        // --- Arrange (Preparar) ---
        var (userId, token) = await _factory.CreateUserAndGetTokenAsync(name: "User", email: "pass.wrong@email.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new CambiarPasswordDto
        {
            OldPassword = "ESTA-ES-INCORRECTA", // <-- Contraseña incorrecta
            NewPassword = "PasswordNueva456!",
            ConfirmPassword = "PasswordNueva456!"
        };

        // --- Act (Actuar) ---
        var response = await _client.PutAsJsonAsync($"/api/{ApiVersion}/profile/change-password", dto);

        // --- Assert (Verificar) ---
        // 1. Verifica la respuesta HTTP 400
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        result.Should().NotBeNull();
        result["message"].Should().Be("La contraseña actual es incorrecta.");
    }

    /// <summary>
    /// Prueba que el endpoint devuelva 400 Bad Request si las nuevas contraseñas no coinciden,
    /// verificando la validación del DTO (FluentValidation).
    /// </summary>
    [Fact]
    public async Task ChangePassword_WithMismatchedNewPasswords_ShouldReturnBadRequest()
    {
        // --- Arrange (Preparar) ---
        var (userId, token) = await _factory.CreateUserAndGetTokenAsync("User", "pass.mismatch@email.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var dto = new CambiarPasswordDto
        {
            OldPassword = "password123",
            NewPassword = "PasswordNueva456!",
            ConfirmPassword = "NO-COINCIDE" // <-- Contraseñas no coinciden
        };

        // --- Act (Actuar) ---
        var response = await _client.PutAsJsonAsync($"/api/{ApiVersion}/profile/change-password", dto);

        // --- Assert (Verificar) ---
        // 1. Verifica la respuesta HTTP 400
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 2. Verifica el mensaje de error de validación
        var errorString = await response.Content.ReadAsStringAsync();
        errorString.Should().Contain("Las contraseñas nuevas no coinciden.");
    }

    #endregion
}