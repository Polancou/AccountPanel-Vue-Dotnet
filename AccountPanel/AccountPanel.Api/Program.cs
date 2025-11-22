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
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

// Configuración inicial
var builder = WebApplication.CreateBuilder(args);

// Configuración log
builder.Host.UseSerilog((context, configuration) => { configuration.ReadFrom.Configuration(context.Configuration); });

// --- Registro de Servicios de Infraestructura y Aplicación ---
// Le decimos al contenedor: "Cuando un constructor pida IAuthService, entrégale una instancia de AuthService".
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IExternalAuthValidator, GoogleAuthValidator>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddHealthChecks();
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

// --- Configuración de Base de Datos y el Contrato IApplicationDbContext ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// 1. Se registra el DbContext concreto (ApplicationDbContext) para la gestión de migraciones y sesiones.
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
// 2. Se registra la interfaz IApplicationDbContext para que apunte a la implementación concreta.
//    Esto permite que la capa de Application pida IApplicationDbContext y reciba una instancia de ApplicationDbContext.
builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

// --- Configuración de Controladores y Mapeo ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
// AutoMapper buscará perfiles en todos los ensamblados de la aplicación.
builder.Services.AddAutoMapper(cfg => cfg.LicenseKey = builder.Configuration["AutoMapper:Key"],
    AppDomain.CurrentDomain.GetAssemblies());

// --- Configuración de CORS ---
var frontendUrl = builder.Configuration["AppSettings:FrontendBaseUrl"];
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "ClientApp",
        policy =>
        {
            policy.WithOrigins(frontendUrl) 
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

// Configuración de rate limit
builder.Services.AddRateLimiter(options =>
{
    // Configuración por defecto si una petición es rechazada (429 Too Many Requests)
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Configura el limte de peticiones, si no existe, usa 5 por defecto
    var authPermitLimit = builder.Configuration.GetValue<int>("RateLimiting:AuthPermitLimit", 5);
    // Política Estricta para Autenticación
    // Permite 5 intentos cada 60 segundos por dirección IP.
    options.AddFixedWindowLimiter("AuthPolicy", opt =>
    {
        opt.Window = TimeSpan.FromSeconds(60);
        opt.PermitLimit = authPermitLimit;
        opt.QueueLimit = 0; 
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });

    // Política Global (opcional, para el resto de la API)
    // Permite 100 peticiones por minuto por IP.
    options.AddFixedWindowLimiter("GlobalPolicy", opt =>
    {
        opt.Window = TimeSpan.FromSeconds(60);
        opt.PermitLimit = 1000;
        opt.QueueLimit = 2;
    });
});

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
    .AddGoogle(options =>
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

app.UseCors(policyName: "ClientApp");
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});
app.UseStaticFiles();
app.UseRateLimiter();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
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