using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Herencia.Data.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Herencia.Business.Services;

/// <summary>
/// Implementación de <see cref="ITokenService"/>: arma y firma un token JWT para un
/// usuario ya autenticado.
/// </summary>
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Genera y firma un JWT con los claims de identidad y rol del usuario.
    /// </summary>
    public string CrearToken(Usuario usuario)
    {
        try
        {
            // ClaimTypes estándar de .NET: son los que ASP.NET Core reconoce automáticamente
            // y hacen funcionar [Authorize(Roles = "...")] sin configuración adicional.
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Role, usuario.Rol.ToString())
            };

            // Clave simétrica secreta del servidor: nunca debe committearse en texto plano
            // ni enviarse al cliente. Sin ella no se puede firmar ningún token.
            var claveSecreta = _configuration["AppSettings:Token"];

            if (string.IsNullOrWhiteSpace(claveSecreta))
            {
                throw new AutenticacionException(
                    "No se pudo generar el token de autenticacion: falta configuracion del servidor.");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveSecreta));
            var credenciales = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                // Ventana de expiración corta: si el token fuera robado, acota el tiempo en
                // que sigue siendo utilizable. Todavía no hay refresh tokens implementados.
                Expires = DateTime.UtcNow.AddMinutes(30),
                SigningCredentials = credenciales
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
        catch (AutenticacionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // No se expone el detalle técnico: podría revelar configuración de seguridad del servidor.
            throw new AutenticacionException("Ocurrio un error al generar el token de autenticacion.", ex);
        }
    }
}
