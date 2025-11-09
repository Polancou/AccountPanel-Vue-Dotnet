using AccountPanel.Application.Interfaces;
using AccountPanel.Application.Services;
using AccountPanel.Domain.Models;
using AccountPanel.Infrastructure.Data;
using AccountPanel.Infrastructure.Services;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AccountPanel.Api.IntegrationTests;

/// <summary>
/// WebApplicationFactory personalizada para las pruebas de integración.
/// Esta clase es responsable de arrancar una versión en memoria de la API
/// con servicios y una base de datos configurados específicamente para las pruebas.
/// Implementa IAsyncLifetime para gestionar la creación y destrucción de la base de datos
/// una sola vez por cada clase de tests, mejorando el rendimiento.
/// </summary>
public class TestApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>
    /// Conexión a la base de datos SQLite en memoria que se compartirá entre los tests.
    /// </summary>
    private readonly SqliteConnection _connection;

    /// <summary>
    /// Almacena una instancia de IConfiguration construida para el entorno de pruebas.
    /// Esto es crucial para poder leer configuraciones (como claves de licencia o secretos)
    /// que son necesarias para registrar servicios en el contenedor de pruebas.
    /// </summary>
    public IConfiguration Configuration { get; private set; }

    /// <summary>
    /// Constructor de la fábrica de API para pruebas.
    /// </summary>
    public TestApiFactory()
    {
        // Se crea una conexión a una base de datos SQLite en memoria.
        // El modo ":memory:" asegura que la base de datos exista solo mientras la conexión esté abierta,
        // proporcionando un aislamiento completo para las pruebas.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Se construye un objeto IConfiguration para el entorno de pruebas.
        // Esto permite a la Factory acceder a valores de configuración (ej. de appsettings.json)
        // de la misma manera que lo hace la aplicación principal.
        Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json") // Lee la configuración base del proyecto de pruebas.
            .AddUserSecrets<TestApiFactory>() // Permite sobreescribir configuraciones con secretos locales.
            .AddEnvironmentVariables()
            .Build();
    }

    /// <summary>
    /// Este método se invoca para configurar el host de la aplicación de pruebas.
    /// Aquí es donde sobreescribimos los servicios de producción con nuestras versiones de prueba
    /// y reconfiguramos la base de datos para que use la versión en memoria.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // --- Reemplazar la Base de Datos ---
            // Se busca y elimina la configuración del DbContext de producción.
            var descriptor =
                services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Se añade un nuevo DbContext configurado para usar nuestra conexión SQLite en memoria.
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));
            
            // --- Re-registrar Servicios ---
            // Es crucial que el contenedor de DI de las pruebas conozca las mismas interfaces y clases
            // que la aplicación principal, ya que el WebApplicationFactory construye la app desde Program.cs.
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IProfileService, ProfileService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IExternalAuthValidator, GoogleAuthValidator>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());


            // --- Re-registrar Otras Dependencias ---
            // Se registra AutoMapper usando la configuración específica que requiere licencia.
            services.AddAutoMapper(cfg => { cfg.LicenseKey = Configuration["AutoMapper:Key"]; }, AppDomain.CurrentDomain.GetAssemblies());

            // Se registran los validadores de FluentValidation desde la capa de Application.
            services.AddValidatorsFromAssembly(typeof(IApplicationDbContext).Assembly);
        });
    }

    /// <summary>
    /// Este método se ejecuta UNA VEZ antes de que comiencen todos los tests de la clase
    /// que usa esta Factory (gracias a IAsyncLifetime).
    /// </summary>
    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TestApiFactory>>();
        // Asegura que el esquema de la base de datos (las tablas) se cree en la base de datos en memoria.
        await context.Database.EnsureCreatedAsync();

        try
        {
            var hasAdminUser = await context.Usuarios.AnyAsync(u => u.Rol == RolUsuario.Admin);

            if (hasAdminUser) return; // El admin de prueba ya existe

            // Usamos la 'Configuration' de esta TestApiFactory (que ya lee user-secrets)
            var adminEmail = Configuration["AdminUser:Email"];
            var adminPassword = Configuration["AdminUser:Password"];

            if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
            {
                logger.LogError("Secretos de AdminUser no encontrados. Asegúrate de que 'AccountPanel.Api.IntegrationTests' tenga acceso a los user-secrets.");
                return;
            }

            var adminUser = new Usuario(
                nombreCompleto: "Admin de Prueba",
                email: adminEmail,
                numeroTelefono: "0000000000",
                rol: RolUsuario.Admin
            );
            adminUser.EstablecerPasswordHash(BCrypt.Net.BCrypt.HashPassword(adminPassword));

            await context.Usuarios.AddAsync(adminUser);
            await context.SaveChangesAsync();

            logger.LogInformation("Usuario administrador de prueba creado.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ocurrió un error durante el sembrado del administrador de prueba.");
            throw;
        }
    }

    /// <summary>
    /// Este método se ejecuta UNA VEZ después de que han terminado todos los tests de la clase.
    /// Se encarga de cerrar y liberar los recursos de la conexión a la base de datos.
    /// </summary>
    public new async Task DisposeAsync()
    {
        await _connection.CloseAsync();
        await _connection.DisposeAsync();
        await base.DisposeAsync();
    }

    /// <summary>
    /// Método de utilidad para limpiar los datos de la base de datos entre ejecuciones de tests,
    /// asegurando que cada test comience en un estado limpio y predecible.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // Borra todos los datos de las tablas especificadas.
        await context.Database.ExecuteSqlRawAsync("DELETE FROM UserLogins");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM Usuarios");
    }
    
    /// <summary>
    /// Método de utilidad para "sembrar" la base de datos con un usuario y obtener su token.
    /// Esto reduce la duplicación de código en los archivos de prueba.
    /// </summary>
    /// <param name="name">Nombre del usuario a crear.</param>
    /// <param name="email">Email del usuario a crear.</param>
    /// <returns>Una tupla con el ID del usuario creado y su token JWT.</returns>
    public async Task<(int UserId, string Token)> CreateUserAndGetTokenAsync(string name, string email)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var user = new Usuario(nombreCompleto: name, email: email, numeroTelefono: "123456789", rol: RolUsuario.User);
        user.EstablecerPasswordHash(passwordHash: BCrypt.Net.BCrypt.HashPassword("password123"));
        await context.Usuarios.AddAsync(user);
        await context.SaveChangesAsync();

        var token = tokenService.CrearToken(user);
        return (user.Id, token);
    }
    
    /// <summary>
    /// Método de utilidad para "sembrar" la base de datos con un usuario, su rol y obtener su token.
    /// Esto reduce la duplicación de código en los archivos de prueba.
    /// </summary>
    /// <param name="name">Nombre del usuario a crear.</param>
    /// <param name="email">Email del usuario a crear.</param>
    /// <param name="rol">Rol del usuario a crear.</param>
    /// <returns>Una tupla con el ID del usuario creado y su token JWT.</returns>
    public async Task<(int userId, string token)> CreateUserAndGetTokenAsync(string name, string email, RolUsuario rol)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();

        var user = new Usuario(nombreCompleto: name, email: email, numeroTelefono: "123456789", rol: rol);
        user.EstablecerPasswordHash(passwordHash: BCrypt.Net.BCrypt.HashPassword("password123"));
        await context.Usuarios.AddAsync(user);
        await context.SaveChangesAsync();

        var token = tokenService.CrearToken(usuario: user);
        return (user.Id, token);
    }
}