namespace AccountPanel.Domain.Models;

/// <summary>
/// Representa el resultado de una operación de autenticación, encapsulando
/// el estado de éxito, un mensaje y un token opcional.
/// </summary>
public class AuthResult
{
    /// <summary>
    /// Indica si la operación fue exitosa.
    /// </summary>
    public bool Success { get; private set; }

    /// <summary>
    /// Proporciona un mensaje descriptivo sobre el resultado de la operación.
    /// </summary>
    public string Message { get; private set; }

    /// <summary>
    /// Contiene el token JWT si la autenticación fue exitosa.
    /// </summary>
    public string Token { get; private set; }

    private AuthResult(bool success, string message, string token = null)
    {
        Success = success;
        Message = message;
        Token = token;
    }

    /// <summary>
    /// Crea una instancia de un resultado exitoso.
    /// </summary>
    /// <param name="token">El token JWT generado (opcional).</param>
    /// <param name="message">El mensaje de éxito.</param>
    /// <returns>Una instancia de AuthResult que representa el éxito.</returns>
    public static AuthResult Ok(string token, string message = "Operación exitosa.")
    {
        return new AuthResult(true, message, token);
    }

    /// <summary>
    /// Crea una instancia de un resultado fallido.
    /// </summary>
    /// <param name="message">El mensaje de error que describe la falla.</param>
    /// <returns>Una instancia de AuthResult que representa la falla.</returns>
    public static AuthResult Fail(string message)
    {
        return new AuthResult(false, message);
    }
}