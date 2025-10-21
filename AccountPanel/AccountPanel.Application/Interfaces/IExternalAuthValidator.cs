namespace AccountPanel.Application.Interfaces;

/// <summary>
/// Contiene la información de un usuario obtenida de un proveedor de autenticación externo.
/// </summary>
public class ExternalAuthUserInfo
{
    /// El ID único del usuario en el proveedor externo (ej. el 'sub' de Google).
    public string ProviderSubjectId { get; set; }
    /// El email del usuario según el proveedor.
    public string Email { get; set; }
    /// El nombre completo del usuario según el proveedor.
    public string Name { get; set; }
}

/// <summary>
/// Define el contrato para los servicios que validan tokens de proveedores externos.
/// </summary>
public interface IExternalAuthValidator
{
    /// <summary>
    /// Valida un token de ID de un proveedor externo y extrae la información del usuario.
    /// </summary>
    /// <param name="idToken">El token de ID proporcionado por el cliente.</param>
    /// <returns>La información del usuario si el token es válido, o null si es inválido.</returns>
    Task<ExternalAuthUserInfo> ValidateTokenAsync(string idToken);
}