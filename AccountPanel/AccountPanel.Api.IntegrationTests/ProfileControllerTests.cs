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
}