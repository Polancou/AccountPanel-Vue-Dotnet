using System.Diagnostics;
using System.Net;
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
            await next(context);
        }
        catch (Exception ex)
        {
            // Loguea el error con todos sus detalles.
            logger.LogError(ex, "Ocurrió una excepción no controlada: {Message}", ex.Message);

            // Genera la respuesta HTTP.
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Genera una respuesta JSON estandarizada para la excepción capturada.
    /// </summary>
    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        // Lógica para desenpaquetar excepciones agregadas (común en async)
        var exceptionToHandle = exception;
        if (exception is AggregateException aggregateException && aggregateException.InnerExceptions.Any())
        {
            exceptionToHandle = aggregateException.InnerExceptions.First();
        }

        HttpStatusCode statusCode;
        string message;

        // Determina el código de estado y mensaje según el tipo de excepción
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

        context.Response.StatusCode = (int)statusCode;

        // Objeto de respuesta estandarizado
        var response = new
        {
            StatusCode = context.Response.StatusCode,
            Message = message,
            TraceId = Activity.Current?.Id ?? context.TraceIdentifier
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}