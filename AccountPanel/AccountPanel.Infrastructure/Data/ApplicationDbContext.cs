using AccountPanel.Application.Interfaces;
using AccountPanel.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AccountPanel.Infrastructure.Data;

/// <summary>
/// Representa la sesión con la base de datos y actúa como el puente principal
/// entre las entidades del dominio y la base de datos subyacente.
/// Proporciona acceso a las colecciones de entidades a través de propiedades DbSet.
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    /// <summary>
    /// Inicializa una nueva instancia de ApplicationDbContext.
    /// </summary>
    /// <param name="options">Las opciones a ser utilizadas por el DbContext,
    /// como la cadena de conexión y el proveedor de base de datos.</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Obtiene o establece un DbSet que representa la tabla de 'Usuarios' en la base de datos.
    /// A través de esta propiedad, se pueden realizar operaciones CRUD (Crear, Leer, Actualizar, Borrar)
    /// sobre los registros de usuarios.
    /// Se marca como 'virtual' para permitir que sea simulado (mockeado) en las pruebas unitarias.
    /// </summary>
    public virtual DbSet<Usuario> Usuarios { get; set; }

    /// <summary>
    /// Obtiene o establece un DbSet que representa la tabla de 'UserLogins' en la base de datos.
    /// Esta tabla almacena los vínculos entre los usuarios del sistema y sus inicios de sesión
    /// a través de proveedores externos (como Google).
    /// Se marca como 'virtual' para permitir que sea simulado (mockeado) en las pruebas unitarias.
    /// </summary>
    public virtual DbSet<UserLogin> UserLogins { get; set; }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        // Inicia una nueva transacción de base de datos
        return await Database.BeginTransactionAsync(cancellationToken);
    }
    
    /// <summary>
    /// Se invoca cuando el modelo para un contexto derivado está siendo creado.
    /// Este método permite una configuración más detallada del modelo de base de datos
    /// utilizando la API Fluida (Fluent API) de Entity Framework Core.
    /// </summary>
    /// <param name="modelBuilder">El constructor que se utiliza para construir el modelo para este contexto.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Es una buena práctica llamar primero a la implementación base.
        base.OnModelCreating(modelBuilder);

        // Se configura la clave primaria compuesta para la entidad UserLogin.
        // Esto asegura la unicidad de los registros, garantizando que no puedan existir
        // dos entradas con la misma combinación de proveedor y clave de proveedor.
        // Por ejemplo, no puede haber dos inicios de sesión de 'Google' con el mismo ID de usuario de Google.
        modelBuilder.Entity<UserLogin>()
            .HasKey(l => new { l.LoginProvider, l.ProviderKey });
        
        // Configuración para la entidad Usuario
        var usuarioEntity = modelBuilder.Entity<Usuario>();

        // Le dice a EF Core que RowVersion es un token de concurrencia
        // SQL Server entiende esto nativamente
        usuarioEntity.Property(u => u.RowVersion)
            .IsRowVersion();
    }
}