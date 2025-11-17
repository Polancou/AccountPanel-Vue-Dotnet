using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AccountPanel.Infrastructure.Data;

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
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddUserSecrets<ApplicationDbContext>() 
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        optionsBuilder.UseSqlServer(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}