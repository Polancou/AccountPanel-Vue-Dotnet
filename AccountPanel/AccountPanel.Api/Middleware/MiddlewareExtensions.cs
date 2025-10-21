namespace AccountPanel.Api.Middleware;

public static class MiddlewareExtensions
{
    /// <summary>
    /// Método de extensión para registrar el GlobalExceptionHandlerMiddleware en el pipeline de la aplicación.
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}