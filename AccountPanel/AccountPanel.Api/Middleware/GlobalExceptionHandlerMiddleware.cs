using System.Diagnostics;
using System.Net;
using System.Text.Json;

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
        // Se establece el código de estado de la respuesta a 500 Internal Server Error.
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";

        // Se crea un objeto anónimo con un mensaje de error genérico y seguro para el cliente.
        // Nunca se debe exponer el mensaje real de la excepción en producción.
        var response = new
        {
            StatusCode = context.Response.StatusCode,
            Message = "Ocurrió un error interno en el servidor. Por favor, intente de nuevo más tarde.",
            TraceId = Activity.Current?.Id ?? context.TraceIdentifier
        };

        // Se serializa el objeto a JSON y se escribe en el cuerpo de la respuesta.
        var jsonResponse = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(jsonResponse);
    }
}