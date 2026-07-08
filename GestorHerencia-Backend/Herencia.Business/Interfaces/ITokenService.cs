using Herencia.Data.Models;

namespace Herencia.Business.Interfaces;

// ITokenService es el CONTRATO publico para la EMISION de credenciales de
// sesion (Tokens JWT) una vez que un Usuario ya fue autenticado con exito
// (tipicamente, despues de que ISeguridadService.VerificarPasswordHash devolvio
// true en un futuro flujo de Login). Se separa de ISeguridadService porque son
// dos preocupaciones de seguridad DISTINTAS: una verifica "quien sos" (hash de
// contrasena), la otra emite "una credencial que demuestra, ante requests
// futuros, que ya demostraste quien sos" (el token).
public interface ITokenService
{
    // Genera un Token JWT (JSON Web Token) firmado digitalmente, que representa
    // la identidad del Usuario recibido. Recibe la entidad "Usuario" completa
    // (no un DTO) porque, al ser un servicio utilitario interno de Business
    // (nunca se expone directamente a la Api), no hay riesgo de "fuga" de
    // campos sensibles: este metodo solo LEE el Id y el Email del usuario para
    // armar los Claims, nunca toca ni expone PasswordHash/PasswordSalt.
    string CrearToken(Usuario usuario);
}
