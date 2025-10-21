using System.ComponentModel.DataAnnotations;

namespace AccountPanel.Domain.Models;

/// <summary>
/// Representa un método de inicio de sesión de un proveedor externo (como Google o Apple)
/// asociado a un Usuario de nuestro sistema.
/// </summary>
public class UserLogin
{
    // El nombre del proveedor (ej. "Google", "Apple"). Parte de la clave primaria compuesta.
    [Key] public string LoginProvider { get; private set; }

    // El ID único que el proveedor externo le asigna al usuario. Parte de la clave primaria compuesta.
    [Key] public string ProviderKey { get; private set; }

    // Clave foránea que vincula este login con nuestro registro de Usuario.
    public int UsuarioId { get; private set; }

    // Propiedad de navegación para que EF Core pueda cargar el objeto Usuario relacionado.
    public Usuario Usuario { get; private set; }

    /// <summary>
    /// Constructor privado sin parámetros, requerido por Entity Framework Core para la materialización de objetos.
    /// </summary>
    private UserLogin()
    {
    }

    /// <summary>
    /// Constructor público para crear un nuevo registro de UserLogin de forma controlada.
    /// </summary>
    /// <param name="loginProvider">El nombre del proveedor, ej. "Google".</param>
    /// <param name="providerKey">El identificador único del usuario en ese proveedor.</param>
    /// <param name="usuario">La entidad Usuario de nuestro sistema a la que se asocia este login.</param>
    public UserLogin(string loginProvider, string providerKey, Usuario usuario)
    {
        // Validaciones para asegurar la integridad del objeto al momento de su creación.
        if (string.IsNullOrWhiteSpace(loginProvider))
            throw new ArgumentNullException(nameof(loginProvider));

        if (string.IsNullOrWhiteSpace(providerKey))
            throw new ArgumentNullException(nameof(providerKey));

        if (usuario is null)
            throw new ArgumentNullException(nameof(usuario));

        LoginProvider = loginProvider;
        ProviderKey = providerKey;
        Usuario = usuario;
        UsuarioId = usuario.Id; // Se asigna el ID del objeto Usuario proporcionado.
    }
}