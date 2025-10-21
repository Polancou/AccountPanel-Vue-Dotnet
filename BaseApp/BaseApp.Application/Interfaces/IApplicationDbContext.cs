using BaseApp.Domain.Models;
using Microsoft.EntityFrameworkCore;

// Asegúrate de tener el using a tu proyecto Domain

namespace BaseApp.Application.Interfaces;

public interface IApplicationDbContext
{
    // Expone solo las colecciones de datos que la aplicación necesita
    DbSet<Usuario> Usuarios { get; }
    DbSet<UserLogin> UserLogins { get; }

    // Expone la habilidad de guardar cambios
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}