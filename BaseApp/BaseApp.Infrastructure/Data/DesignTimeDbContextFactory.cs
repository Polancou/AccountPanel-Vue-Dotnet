using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BaseApp.Infrastructure.Data;

/// <summary>
/// Fábrica para la creación de instancias de ApplicationDbContext en tiempo de diseño.
/// Esta clase es utilizada exclusivamente por las herramientas de la línea de comandos de Entity Framework Core
/// (por ejemplo, al ejecutar 'dotnet ef migrations add'). Su propósito es desacoplar la creación del DbContext
/// de la configuración de arranque de la aplicación principal (Program.cs), evitando así problemas
/// cuando el host de la aplicación requiere servicios o configuraciones no disponibles durante el diseño.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    /// <summary>
    /// Crea y configura una nueva instancia de ApplicationDbContext.
    /// Este método es invocado por las herramientas de EF Core para obtener el contexto de la base de datos.
    /// </summary>
    /// <param name="args">Argumentos de la línea de comandos (generalmente no se utilizan en este contexto).</param>
    /// <returns>Una nueva instancia de ApplicationDbContext configurada para las migraciones.</returns>
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Se construye un objeto IConfiguration para leer el archivo appsettings.json.
        // Esto permite a las herramientas de diseño encontrar la cadena de conexión
        // sin necesidad de iniciar todo el host de la aplicación.
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();
        // Se crea un constructor de opciones para el DbContext.
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
        // Se obtiene la cadena de conexión "DefaultConnection" desde la configuración.
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        // Se configuran las opciones del DbContext para que utilice SQLite con la cadena de conexión obtenida.
        builder.UseSqlite(connectionString);
        // Se devuelve una nueva instancia del ApplicationDbContext con las opciones configuradas.
        return new ApplicationDbContext(builder.Options);
    }
}