using System.Diagnostics;
using System.Net;
using System.Text.Json;
using AccountPanel.Application.Exceptions;

namespace AccountPanel.Api.Middleware;

/// <summary>
/// Middleware para capturar de forma global cualquier excepción no controlada
/// que ocurra durante el procesamiento de una petición.
/// </summary>
public class GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
{
    /// <summary>
    /// Método principal del middleware que se invoca en cada petición.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Intenta ejecutar el siguiente middleware en la cadena.
            // Si no hay ninguna excepción, este middleware no hace nada más.
            await next(context);
        }
        catch (Exception ex)
        {
            // Si ocurre cualquier excepción, se captura aquí.
            // Se loguea el error con todos sus detalles para poder depurarlo.
            logger.LogError(ex, "Ocurrió una excepción no controlada: {Message}", ex.Message);

            // Se llama al método que se encargará de generar la respuesta HTTP.
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Genera una respuesta JSON estandarizada para la excepción capturada.
    /// </summary>
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";

        // --- AÑADIR LÓGICA DE DESENCRIPTADO ---
        // Si la excepción es una AggregateException (común en tareas async),
        // usamos la primera excepción interna que es la que nos interesa.
        var exceptionToHandle = exception;
        if (exception is AggregateException aggregateException && aggregateException.InnerExceptions.Any())
        {
            exceptionToHandle = aggregateException.InnerExceptions.First();
        }
        // --- FIN DE LÓGICA DE DESENCRIPTADO ---

        HttpStatusCode statusCode;
        string message;

        // Ahora, usamos 'exceptionToHandle' en el switch
        switch (exceptionToHandle)
        {
            case NotFoundException:
                statusCode = HttpStatusCode.NotFound;
                message = exceptionToHandle.Message;
                break;
            case ValidationException:
                statusCode = HttpStatusCode.BadRequest;
                message = exceptionToHandle.Message;
                break;
            default:
                statusCode = HttpStatusCode.InternalServerError;
                message = "Ocurrió un error interno en el servidor. Por favor, intente de nuevo más tarde.";
                break;
        }

        context.Response.StatusCode = (int)statusCode; // Asigna el código correcto

        var response = new
        {
            StatusCode = context.Response.StatusCode,
            Message = message, // Usa el mensaje determinado
            TraceId = Activity.Current?.Id ?? context.TraceIdentifier
        };

        var jsonResponse = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(jsonResponse);
    }
}