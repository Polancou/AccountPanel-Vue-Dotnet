using System.ComponentModel.DataAnnotations;

namespace AccountPanel.Domain.Models;

/// <summary>
/// Define los roles que un usuario puede tener en el sistema.
/// </summary>
public enum RolUsuario
{
    User,
    Admin
}

/// <summary>
/// Representa la entidad principal de un usuario en la aplicación.
/// Esta clase encapsula todos los datos y comportamientos de un usuario.
/// </summary>
public class Usuario
{
    #region Propiedades

    [Key] 
    public int Id { get; private set; }

    [Required] [MaxLength(100)] 
    public string NombreCompleto { get; private set; }

    [Required] [EmailAddress] [MaxLength(100)] 
    public string Email { get; private set; }

    /// <summary>
    /// El hash de la contraseña. Es opcional ('nullable') para permitir
    /// inicios de sesión de terceros (ej. Google) que no usan contraseña en nuestro sistema.
    /// </summary>
    public string? PasswordHash { get; private set; }
    /// <summary>
    /// El número de teléfono del usuario.
    /// </summary>
    [Required] [MaxLength(20)] 
    public string NumeroTelefono { get; private set; }
    /// <summary>
    /// El rol del usuario.
    /// </summary>
    [Required] 
    public RolUsuario Rol { get; private set; }
    /// <summary>
    /// La fecha de registro del usuario.
    /// </summary>
    public DateTime FechaRegistro { get; private set; }
    /// <summary>
    /// La URL de la imagen de perfil del usuario.
    /// </summary>
    public string? AvatarUrl { get; private set; }

    #endregion

    #region Constructores

    /// <summary>
    /// Constructor privado sin parámetros, requerido por Entity Framework Core.
    /// </summary>
    private Usuario()
    {
    }

    /// <summary>
    /// Constructor público para crear nuevos usuarios de forma controlada y válida.
    /// </summary>
    public Usuario(string nombreCompleto, string email, string numeroTelefono, RolUsuario rol)
    {
        if (string.IsNullOrWhiteSpace(nombreCompleto))
            throw new ArgumentNullException(nameof(nombreCompleto), "El nombre completo es obligatorio.");

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentNullException(nameof(email), "El email es obligatorio.");

        NombreCompleto = nombreCompleto;
        Email = email;
        NumeroTelefono = numeroTelefono;
        Rol = rol;
        FechaRegistro = DateTime.UtcNow;
    }

    #endregion

    #region Métodos de Modificación

    /// <summary>
    /// Establece o actualiza el hash de la contraseña del usuario.
    /// </summary>
    public void EstablecerPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentNullException(nameof(passwordHash), "El hash de la contraseña no puede estar vacío.");

        PasswordHash = passwordHash;
    }

    /// <summary>
    /// Actualiza el número de teléfono del usuario de forma individual.
    /// </summary>
    public void ActualizarNumeroTelefono(string nuevoNumero)
    {
        if (string.IsNullOrWhiteSpace(nuevoNumero))
            throw new ArgumentException("El nuevo número de teléfono no puede estar vacío.", nameof(nuevoNumero));

        NumeroTelefono = nuevoNumero;
    }
    
    /// <summary>
    /// Actualiza la información del perfil del usuario (nombre y teléfono).
    /// </summary>
    public void ActualizarPerfil(string nuevoNombre, string nuevoNumero)
    {
        if (!string.IsNullOrWhiteSpace(nuevoNombre))
        {
            NombreCompleto = nuevoNombre;
        }
        if (!string.IsNullOrWhiteSpace(nuevoNumero))
        {
            NumeroTelefono = nuevoNumero;
        }
    }
    
    /// <summary>
    /// Actualiza la URL de la imagen de perfil del usuario.
    /// </summary>
    public void SetAvatarUrl(string nuevoUrl)
    {
        if (!string.IsNullOrWhiteSpace(nuevoUrl))
        {
            AvatarUrl = nuevoUrl;
        }
    }

    #endregion
}