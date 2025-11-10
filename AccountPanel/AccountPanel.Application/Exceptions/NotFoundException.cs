namespace AccountPanel.Application.Exceptions;

/// <summary>
/// Excepción para escenarios de "recurso no encontrado" (HTTP 404).
/// </summary>
public class NotFoundException(string message) : Exception(message)
{
}