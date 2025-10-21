using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AccountPanel.Application.Interfaces;
using AccountPanel.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AccountPanel.Infrastructure.Services;

public class TokenService(IConfiguration config) : ITokenService
{
    public string CrearToken(Usuario usuario)
    {
        // Los 'claims' son piezas de información sobre el usuario que queremos incluir en el token.
        var claims = new List<Claim>
        {
            // El ID del usuario es el claim más importante para identificarlo.
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            // También incluimos el email.
            new Claim(ClaimTypes.Email, usuario.Email),
            // Y el rol, para poder hacer autorizaciones en el futuro.
            new Claim(ClaimTypes.Role, usuario.Rol.ToString())
        };

        // Obtenemos la clave secreta desde nuestra configuración.
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]));
        // Creamos las credenciales de firma usando la clave y el algoritmo de seguridad más fuerte.
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        // Creamos el descriptor del token, que une toda la información.
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims), // Los claims que definimos arriba.
            Expires = DateTime.Now.AddDays(7), // El token expirará en 7 días.
            SigningCredentials = creds, // Nuestras credenciales de firma.
            Issuer = config["Jwt:Issuer"] // Quién emite el token.
        };

        // Usamos un manejador para crear el token basado en el descriptor.
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        // Finalmente, escribimos el token como un string. Este es el string que enviaremos al cliente.
        return tokenHandler.WriteToken(token);
    }
}