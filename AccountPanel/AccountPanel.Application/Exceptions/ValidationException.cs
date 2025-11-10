namespace AccountPanel.Application.Exceptions;

/// <summary>
/// Excepción para fallos de validación de negocio (HTTP 400).
/// </summary>
public class ValidationException(string message) : Exception(message)
{
}