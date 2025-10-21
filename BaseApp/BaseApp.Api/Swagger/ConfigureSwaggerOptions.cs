using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BaseApp.Api.Swagger;

public class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        // Itera sobre todas las versiones de la API descubiertas (ej. v1, v2).
        foreach (var description in provider.ApiVersionDescriptions)
        {
            // Crea un nuevo documento de Swagger para cada versión.
            options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
        }
    }

    private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
    {
        var info = new OpenApiInfo
        {
            Title = "BaseApp API",
            Version = description.ApiVersion.ToString(),
            Description = "Una plantilla de API profesional y robusta."
        };

        if (description.IsDeprecated)
        {
            info.Description += " (Esta versión de la API está obsoleta).";
        }

        return info;
    }
}