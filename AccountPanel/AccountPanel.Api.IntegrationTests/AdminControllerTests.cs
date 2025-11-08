using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AccountPanel.Application.DTOs;
using AccountPanel.Domain.Models;
using FluentAssertions;

namespace AccountPanel.Api.IntegrationTests;

/// <summary>
/// Contiene las pruebas de integración para el AdminController,
/// verificando especialmente la autorización por roles.
/// </summary>
public class AdminControllerTests : IClassFixture<TestApiFactory>, IAsyncLifetime
{
    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;
    private const string ApiVersion = "v1";

    public AdminControllerTests(TestApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Prueba el "camino feliz": un usuario con rol 'Admin'
    /// debería poder acceder al endpoint y obtener la lista de usuarios.
    /// </summary>
    [Fact]
    public async Task GetAllUsers_WithAdminToken_ShouldReturnOkAndUserList()
    {
        // --- Arrange (Preparar) ---
        // 1. Crea un usuario Admin y obtén su token
        var (adminId, adminToken) = await _factory.CreateUserAndGetTokenAsync(
            name: "Admin", email: "admin@test.com", rol: RolUsuario.Admin);

        // 2. Crea un usuario regular (solo para que la lista no esté vacía)
        await _factory.CreateUserAndGetTokenAsync(
            name: "User", email: "user@test.com", rol: RolUsuario.User);

        // 3. Configura el cliente HTTP con el token de Admin
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme: "Bearer", parameter: adminToken);

        // --- Act (Actuar) ---
        var response = await _client.GetAsync(requestUri: $"/api/{ApiVersion}/admin/users");

        // --- Assert (Verificar) ---
        // 1. Verifica que la respuesta sea 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Verifica que el cuerpo de la respuesta contenga la lista de 2 usuarios
        var userList = await response.Content.ReadFromJsonAsync<List<PerfilUsuarioDto>>();
        userList.Should().NotBeNull();
        userList.Should().HaveCount(expected: 2);
        userList.Should().Contain(u => u.Email == "admin@test.com" && u.Rol == "Admin");
    }

    /// <summary>
    /// Prueba que un usuario con rol 'User' reciba un 403 Forbidden
    /// al intentar acceder al endpoint de admin.
    /// </summary>
    [Fact]
    public async Task GetAllUsers_WithUserToken_ShouldReturnForbidden()
    {
        // --- Arrange (Preparar) ---
        // 1. Crea un usuario regular (User) y obtén su token
        var (userId, userToken) = await _factory.CreateUserAndGetTokenAsync(
            name: "Regular User", email: "user@test.com", rol: RolUsuario.User);

        // 2. Configura el cliente HTTP con el token de User
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme: "Bearer", parameter: userToken);

        // --- Act (Actuar) ---
        var response = await _client.GetAsync(requestUri: $"/api/{ApiVersion}/admin/users");

        // --- Assert (Verificar) ---
        // Verifica que la respuesta sea 403 Forbidden
        response.StatusCode.Should().Be(expected: HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Prueba que un usuario sin token (no autenticado) reciba un 401 Unauthorized
    /// al intentar acceder al endpoint de admin.
    /// </summary>
    [Fact]
    public async Task GetAllUsers_WithoutToken_ShouldReturnUnauthorized()
    {
        // --- Arrange (Preparar) ---
        _client.DefaultRequestHeaders.Authorization = null;

        // --- Act (Actuar) ---
        var response = await _client.GetAsync(requestUri: $"/api/{ApiVersion}/admin/users");

        // --- Assert (Verificar) ---
        // Verifica que la respuesta sea 401 Unauthorized
        response.StatusCode.Should().Be(expected: HttpStatusCode.Unauthorized);
    }
}