using BaseApp.Application.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace BaseApp.Infrastructure.Services;

public class GoogleAuthValidator : IExternalAuthValidator
{
    private readonly IConfiguration _configuration;

    public GoogleAuthValidator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<ExternalAuthUserInfo> ValidateTokenAsync(string idToken)
    {
        var googleClientId = _configuration["Authentication:Google:ClientId"];
        var settings = new GoogleJsonWebSignature.ValidationSettings()
        {
            Audience = new List<string> { googleClientId }
        };

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            return new ExternalAuthUserInfo
            {
                ProviderSubjectId = payload.Subject,
                Email = payload.Email,
                Name = payload.Name
            };
        }
        catch (InvalidJwtException)
        {
            // Si el token no es válido, la librería lanza una excepción.
            // La manejamos devolviendo null para indicar el fallo.
            return null;
        }
    }
}