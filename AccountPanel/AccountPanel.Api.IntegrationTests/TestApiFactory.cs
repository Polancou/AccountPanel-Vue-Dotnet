using AccountPanel.Application.Interfaces;
using AccountPanel.Application.Services;
using AccountPanel.Domain.Models;
using AccountPanel.Infrastructure.Data;
using AccountPanel.Infrastructure.Services;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AccountPanel.Api.IntegrationTests;

public class TestApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>
    /// Cadena de conexión a la base de datos 'master' de SQL Edge,
    /// usada para crear y eliminar la base de datos de prueba.
    /// </summary>
    private readonly string _masterConnectionString;
    
    /// <summary>
    /// Cadena de conexión a la base de datos de prueba única
    /// (ej. "accountpanel_test_...").
    /// </summary>
    private readonly string _testConnectionString;
    
    /// <summary>
    /// Nombre único generado aleatoriamente para la base de datos de esta
    /// ejecución de pruebas, garantizando aislamiento total.
    /// </summary>
    private readonly string _dbName = $"accountpanel_test_{Guid.NewGuid():N}";
    
    /// <summary>
    /// Configuración de la aplicación (leída desde user-secrets).
    /// </summary>
    public IConfiguration Configuration { get; private set; }

    /// <summary>
    /// Constructor de la fábrica de API para pruebas.
    /// Se ejecuta UNA SOLA VEZ al inicio de la suite de tests.
    /// </summary>
    public TestApiFactory()
    {
        // --- 1. Construir la Configuración ---
        Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddUserSecrets<Program>() 
            .Build();

        // --- 2. Leer la Cadena de Conexión 'master' ---
        _masterConnectionString = Configuration.GetConnectionString("TestConnection");

        if (string.IsNullOrEmpty(_masterConnectionString))
        {
            throw new Exception("No se encontró 'ConnectionStrings:TestConnection' en los user-secrets.");
        }

        // --- 3. Construir la Cadena de Conexión de Prueba (Dinámicamente) ---
        // Usa SqlConnectionStringBuilder para tomar la cadena 'master'
        // y solo cambiar el nombre de la base de datos.
        var testBuilder = new SqlConnectionStringBuilder(_masterConnectionString)
        {
            // Reemplaza 'master' por el nombre de nuestra BD de prueba única
            InitialCatalog = _dbName 
        };
        _testConnectionString = testBuilder.ConnectionString;
    }

    /// <summary>
    /// Este método se invoca para configurar el host de la aplicación de pruebas.
    /// Aquí es donde sobreescribimos los servicios de producción con nuestras versiones de prueba
    /// (como la base de datos y IEmailService).
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // --- 1. Reemplazar la Base de Datos ---
            // Elimina todos los registros de DbContext (incluyendo SqlServer de Program.cs)
            var dbContextRegistrations = services.Where(
                d => d.ServiceType == typeof(ApplicationDbContext) || 
                     d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                     d.ServiceType == typeof(IApplicationDbContext)
            ).ToList();

            foreach (var descriptor in dbContextRegistrations)
            {
                services.Remove(descriptor);
            }

            // Añade el DbContext usando UseSqlServer con la CADENA DE PRUEBA dinámica
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(_testConnectionString);
            });
            
            // Registra la interfaz (UNA SOLA VEZ)
            services.AddScoped<IApplicationDbContext>(provider => 
                provider.GetRequiredService<ApplicationDbContext>());

            // --- 2. Mock de IEmailService ---
            // Elimina el servicio de email real (Mailtrap/SendGrid)
            var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailService));
            if (emailDescriptor != null)
            {
                services.Remove(emailDescriptor);
            }
            
            // Añade un Mock que "finge" enviar correos exitosamente
            var mockEmailService = new Mock<IEmailService>();
            mockEmailService
                .Setup(s => s.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            mockEmailService
                .Setup(s => s.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            services.AddSingleton(mockEmailService.Object);

            // --- 3. Re-registrar Servicios ---
            // Vuelve a registrar todos los servicios de la aplicación
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IProfileService, ProfileService>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IExternalAuthValidator, GoogleAuthValidator>();
            services.AddScoped<IAdminService, AdminService>();
            services.AddScoped<IFileStorageService, FileStorageService>();

            // --- 4. Re-registrar Otras Dependencias ---
            services.AddAutoMapper(cfg => { cfg.LicenseKey = Configuration["AutoMapper:Key"]; },
                AppDomain.CurrentDomain.GetAssemblies());
            services.AddValidatorsFromAssembly(typeof(IApplicationDbContext).Assembly);
        });
    }

    /// <summary>
    /// Se ejecuta UNA VEZ al inicio de toda la suite de tests.
    /// Crea la base de datos de prueba única y aplica las migraciones.
    /// </summary>
    public async Task InitializeAsync()
    {
        // 1. Conéctate a 'master' y CREA la base de datos de prueba
        await using (var connection = new SqlConnection(_masterConnectionString))
        {
            await connection.OpenAsync();
            await using (var command = connection.CreateCommand())
            {
                command.CommandText = $"CREATE DATABASE [{_dbName}]";
                await command.ExecuteNonQueryAsync();
            }
        }

        // 2. Ahora conéctate a la NUEVA base de datos y aplica las migraciones
        using (var scope = Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync(); // Aplica las migraciones de SQL Server
        }
    }

    /// <summary>
    /// Se ejecuta UNA VEZ al final de toda la suite de tests.
    /// Destruye la base de datos de prueba única para limpiar.
    /// </summary>
    public new async Task DisposeAsync()
    {
        // 1. Conéctate a 'master' y BORRA la base de datos de prueba
        await using (var connection = new SqlConnection(_masterConnectionString))
        {
            await connection.OpenAsync();
            await using (var command = connection.CreateCommand())
            {
                // Cierra forzosamente todas las conexiones activas a la BD de prueba
                command.CommandText = $"ALTER DATABASE [{_dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
                await command.ExecuteNonQueryAsync();
                
                // Borra la base de datos
                command.CommandText = $"DROP DATABASE [{_dbName}]";
                await command.ExecuteNonQueryAsync();
            }
        }
        
        await base.DisposeAsync();
    }
    
    /// <summary>
    /// Método de utilidad para limpiar TODAS las tablas *entre* tests,
    /// asegurando que cada test comience con una base de datos vacía.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Ejecuta T-SQL para deshabilitar constraints, borrar datos y rehabilitar constraints
        await context.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT all'");
        await context.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'DELETE FROM ?'");
        await context.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all'");
    }

    /// <summary>
    /// Método helper para crear un usuario 'User' estándar.
    /// </summary>
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
    /// Método helper para crear un usuario con un rol específico (ej. 'Admin').
    /// </summary>
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