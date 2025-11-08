// --- LIBRERÍAS DE SISTEMA Y MICROSOFT ---

using System.Text;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using AccountPanel.Api.Middleware;
using AccountPanel.Api.Swagger;
using AccountPanel.Application.Interfaces;
using AccountPanel.Application.Services;
using AccountPanel.Infrastructure.Data;
using AccountPanel.Infrastructure.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using AccountPanel.Domain.Models;

// --- LIBRERÍAS DE TERCEROS ---

// --- NAMESPACES DE LA APLICACIÓN (NUEVA ARQUITECTURA) ---
// Se referencian todas las capas para poder registrar sus servicios.

// --- 1. CONFIGURACIÓN INICIAL DE LA APLICACIÓN ---
var builder = WebApplication.CreateBuilder(args);

// --- 2. CONFIGURACIÓN DEL LOGGING ---
builder.Host.UseSerilog((context, configuration) => { configuration.ReadFrom.Configuration(context.Configuration); });

// --- 3. CONFIGURACIÓN DE SERVICIOS (INYECCIÓN DE DEPENDENCIAS) ---
// Esta es la sección más importante para la Arquitectura Limpia.
// Aquí se "enseña" a la aplicación cómo resolver los contratos (interfaces).

// --- Registro de Servicios de Infraestructura y Aplicación ---
// Le decimos al contenedor: "Cuando un constructor pida IAuthService, entrégale una instancia de AuthService".
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IExternalAuthValidator, GoogleAuthValidator>();
builder.Services.AddScoped<IAdminService, AdminService>();

// --- Configuración de Base de Datos y el Contrato IApplicationDbContext ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// 1. Se registra el DbContext concreto (ApplicationDbContext) para la gestión de migraciones y sesiones.
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
// 2. Se registra la interfaz IApplicationDbContext para que apunte a la implementación concreta.
//    Esto permite que la capa de Application pida IApplicationDbContext y reciba una instancia de ApplicationDbContext.
builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

// --- Configuración de Controladores y Mapeo ---
builder.Services.AddControllers();
// AutoMapper buscará perfiles en todos los ensamblados de la aplicación.
builder.Services.AddAutoMapper(cfg => cfg.LicenseKey = builder.Configuration["AutoMapper:Key"],
    AppDomain.CurrentDomain.GetAssemblies());

// --- Configuración de Validación con FluentValidation ---
// Se leen los validadores desde el ensamblado de la capa de Application.
builder.Services.AddValidatorsFromAssembly(typeof(IApplicationDbContext).Assembly);
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

// --- Configuración de Autenticación (JWT y Google) ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = false
        };
    })
    .AddGoogle(options => // Este paquete (Microsoft.AspNetCore.Authentication.Google) es correcto en la capa de API.
    {
        var googleAuthNSection = builder.Configuration.GetSection("Authentication:Google");
        options.ClientId = googleAuthNSection["ClientId"];
        options.ClientSecret = googleAuthNSection["ClientSecret"];
    });

// --- Configuración de Versionado de API ---
builder.Services.AddApiVersioning(options =>
    {
        options.ReportApiVersions = true;
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.DefaultApiVersion = new ApiVersion(majorVersion: 1, minorVersion: 0);
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

// --- Configuración de Documentación de API (Swagger/Scalar) ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen();
builder.Services.AddFluentValidationRulesToSwagger();

// --- 4. CONSTRUCCIÓN DE LA APLICACIÓN ---
var app = builder.Build();

// --- 5. CONFIGURACIÓN DEL PIPELINE DE PETICIONES HTTP ---

// Se inicializa el seeding de usuario administrador.
await SeedAdminUserAsync(services: app.Services);
// El orden de los middlewares en esta sección es MUY importante.
app.UseSerilogRequestLogging();
app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapSwagger("/openapi/{documentName}.json");
    app.MapScalarApiReference();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var descriptions = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        foreach (var description in descriptions.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                description.GroupName.ToUpperInvariant());
        }
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// --- 6. ARRANQUE DE LA APLICACIÓN ---
try
{
    Log.Information("Iniciando la aplicación...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicación no pudo iniciar correctamente.");
}
finally
{
    Log.CloseAndFlush();
}

async Task SeedAdminUserAsync(IServiceProvider services)
{
    // Se crea un scope para obtener las dependencias necesarias para el seeding.
    using var scope = services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    // Verifica si ya existe un administrador en la base de datos.
    var hasAdmin = await context.Usuarios.AnyAsync(u => u.Rol == RolUsuario.Admin);
    // Si ya existe un administrador, no hace nada.
    if (hasAdmin) return;
    // Si no existe, se crea un administrador con los datos de secrets.
    var adminEmail = config["AdminUser:Email"];
    var adminPassword = config["AdminUser:Password"];
    // Verifica que los datos de configuración sean válidos.
    if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
    {
        logger.LogError("No se ha encontrado la configuración de administrador en el archivo de configuración.");
        return;
    }
    
    // Crea un administrador con credenciales de ejemplo.
    var admin = new Usuario(nombreCompleto: "Administrador del Sistema", email: adminEmail, numeroTelefono: "0000000000", rol: RolUsuario.Admin);
    // Hashea la contraseña del administrador.
    admin.EstablecerPasswordHash(passwordHash: BCrypt.Net.BCrypt.HashPassword(adminPassword));
    // Añade el administrador al contexto de la base de datos.
    await context.Usuarios.AddAsync(admin);
    // Guarda los cambios en la base de datos.
    await context.SaveChangesAsync();
    // Registra un mensaje en el log indicando que se ha creado el administrador.
    logger.LogInformation("Se ha creado un usuario administrador.");
}

// --- 7. VISIBILIDAD PARA PROYECTOS EXTERNOS ---
namespace AccountPanel.Api
{
    /// <summary>
    /// La clase 'Program' es el punto de entrada principal de la aplicación.
    /// Esta declaración parcial hace que la clase 'Program' sea visible para otros proyectos,
    /// lo cual es un requisito para que la clase WebApplicationFactory del proyecto de pruebas
    /// de integración (AccountPanel.Api.IntegrationTests) pueda descubrir y arrancar la API en memoria.
    /// </summary>
    public partial class Program
    {
    }
}