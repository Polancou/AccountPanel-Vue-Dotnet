using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AccountPanel.Application.DTOs;
using AccountPanel.Domain.Models;
using FluentAssertions;

namespace AccountPanel.Api.IntegrationTests;

/// <summary>
/// Contiene las pruebas de integración para el AdminController,
/// verificando la autorización y la respuesta paginada.
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
    /// Prepara la base de datos con un número específico de usuarios
    /// y devuelve el token del usuario Admin.
    /// </summary>
    /// <param name="totalUsers">El número total de usuarios a crear (incluyendo al admin).</param>
    /// <returns>El token de autenticación del usuario Admin.</returns>
    private async Task<string> SetupUsersAndGetAdminTokenAsync(int totalUsers)
    {
        // 1. Crea un usuario Admin y obtén su token
        var (adminId, adminToken) = await _factory.CreateUserAndGetTokenAsync(
            name: "Admin", email: "admin@test.com", rol: RolUsuario.Admin);

        // 2. Crea el resto de usuarios regulares
        // (totalUsers - 1 porque el admin ya cuenta como 1)
        var userTasks = new List<Task>();
        for (int i = 1; i < totalUsers; i++)
        {
            userTasks.Add(_factory.CreateUserAndGetTokenAsync(
                name: $"User {i}", email: $"user{i}@test.com", rol: RolUsuario.User));
        }
        await Task.WhenAll(userTasks);

        // 3. Configura el cliente HTTP con el token de Admin
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme: "Bearer", parameter: adminToken);

        return adminToken;
    }

    /// <summary>
    /// Prueba que la página 1 con un tamaño de 10 se devuelva correctamente.
    /// </summary>
    [Fact]
    public async Task GetAllUsers_WithAdminToken_ShouldReturnPagedResult_Page1()
    {
        // --- Arrange (Preparar) ---
        // 1. Crea 15 usuarios en total y obtén el token de Admin
        await SetupUsersAndGetAdminTokenAsync(totalUsers: 15);
        int pageNumber = 1;
        int pageSize = 10;

        // --- Act (Actuar) ---
        var response = await _client.GetAsync(
            requestUri: $"/api/{ApiVersion}/admin/users?pageNumber={pageNumber}&pageSize={pageSize}");

        // --- Assert (Verificar) ---
        // 1. Verifica que la respuesta sea 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Deserializa la respuesta paginada (ajusta PagedResult si tu DTO se llama diferente)
        var pagedResponse = await response.Content.ReadFromJsonAsync<PagedResultDto<PerfilUsuarioDto>>();

        // 3. Verifica las propiedades de la paginación
        pagedResponse.Should().NotBeNull();
        pagedResponse.PageNumber.Should().Be(pageNumber);
        pagedResponse.PageSize.Should().Be(pageSize);
        pagedResponse.TotalCount.Should().Be(15);
        pagedResponse.TotalPages.Should().Be(2); // 15 items / 10 por página = 2 páginas

        // 4. Verifica los items de la página actual
        pagedResponse.Items.Should().NotBeNull();
        pagedResponse.Items.Should().HaveCount(pageSize); // 10 items en la página 1
        pagedResponse.Items.Should().Contain(u => u.Email == "admin@test.com");
    }

    /// <summary>
    /// Prueba que la página 2 con un tamaño de 10 devuelva los items restantes.
    /// </summary>
    [Fact]
    public async Task GetAllUsers_WithAdminToken_ShouldReturnPagedResult_Page2()
    {
        // --- Arrange (Preparar) ---
        // 1. Crea 15 usuarios en total y obtén el token de Admin
        await SetupUsersAndGetAdminTokenAsync(totalUsers: 15);
        int pageNumber = 2;
        int pageSize = 10;

        // --- Act (Actuar) ---
        var response = await _client.GetAsync(
            requestUri: $"/api/{ApiVersion}/admin/users?pageNumber={pageNumber}&pageSize={pageSize}");

        // --- Assert (Verificar) ---
        // 1. Verifica que la respuesta sea 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Deserializa la respuesta paginada
        var pagedResponse = await response.Content.ReadFromJsonAsync<PagedResultDto<PerfilUsuarioDto>>();

        // 3. Verifica las propiedades de la paginación
        pagedResponse.Should().NotBeNull();
        pagedResponse.PageNumber.Should().Be(pageNumber);
        pagedResponse.PageSize.Should().Be(pageSize);
        pagedResponse.TotalCount.Should().Be(15);
        pagedResponse.TotalPages.Should().Be(2);

        // 4. Verifica los items de la página actual (los 5 restantes)
        pagedResponse.Items.Should().NotBeNull();
        pagedResponse.Items.Should().HaveCount(5); // 5 items restantes en la página 2
    }

    /// <summary>
    /// Prueba que un usuario con rol 'User' reciba un 403 Forbidden
    /// al intentar acceder al endpoint paginado.
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
        // Llama al endpoint paginado
        var response = await _client.GetAsync(requestUri: $"/api/{ApiVersion}/admin/users?pageNumber=1&pageSize=10");

        // --- Assert (Verificar) ---
        // Verifica que la respuesta sea 403 Forbidden
        response.StatusCode.Should().Be(expected: HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Prueba que un usuario sin token (no autenticado) reciba un 401 Unauthorized
    /// al intentar acceder al endpoint paginado.
    /// </summary>
    [Fact]
    public async Task GetAllUsers_WithoutToken_ShouldReturnUnauthorized()
    {
        // --- Arrange (Preparar) ---
        _client.DefaultRequestHeaders.Authorization = null;

        // --- Act (Actuar) ---
        // Llama al endpoint paginado
        var response = await _client.GetAsync(requestUri: $"/api/{ApiVersion}/admin/users?pageNumber=1&pageSize=10");

        // --- Assert (Verificar) ---
        // Verifica que la respuesta sea 401 Unauthorized
        response.StatusCode.Should().Be(expected: HttpStatusCode.Unauthorized);
    }
}