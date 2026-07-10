using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Herencia.Data.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Herencia.Business.Services;

// TokenService es la implementacion CONCRETA de ITokenService: sabe COMO armar
// y firmar un Token JWT valido para un Usuario ya autenticado.
//
// --- ¿Que es un JWT y como esta compuesto? ---
// Un JSON Web Token (JWT, RFC 7519) es un string compacto compuesto por TRES
// partes, cada una codificada en Base64Url y separadas por un punto (".");
// visualmente se ve asi:
//
//     xxxxx.yyyyy.zzzzz
//     HEADER.PAYLOAD.SIGNATURE
//
// 1) HEADER: un JSON con metadatos del propio token, tipicamente el algoritmo
//    de firma usado (ej: {"alg":"HS512","typ":"JWT"}). Lo arma automaticamente
//    JwtSecurityTokenHandler a partir de las SigningCredentials que le pasamos.
// 2) PAYLOAD: un JSON con los "Claims" (afirmaciones) sobre el usuario y sobre
//    el propio token: quien es el usuario (Id, Email), cuando fue emitido
//    (iat), y cuando expira (exp). Este JSON viaja SOLO codificado en Base64,
//    NO ENCRIPTADO: cualquiera que intercepte el token puede LEER su
//    contenido (por eso jamas se debe poner informacion secreta, como
//    contrasenas, en un Claim).
// 3) SIGNATURE: el resultado de aplicar el algoritmo de firma (en este caso,
//    HMAC-SHA512) sobre la concatenacion "Base64Url(Header) + '.' +
//    Base64Url(Payload)", usando nuestra clave secreta del servidor
//    (AppSettings:Token). Esta firma es la que garantiza INTEGRIDAD y
//    AUTENTICIDAD: si un atacante modifica un solo caracter del payload (ej:
//    para hacerse pasar por otro usuario cambiando el Id), la firma
//    recalculada del lado del servidor YA NO va a coincidir con la firma
//    original, y el token sera rechazado. La clave secreta NUNCA viaja dentro
//    del token: solo el servidor la conoce, por eso solo el servidor puede
//    emitir y validar firmas legitimas.
public class TokenService : ITokenService
{
    // --- Inyeccion de Dependencias por CONSTRUCTOR ---
    // IConfiguration (Microsoft.Extensions.Configuration) es la abstraccion
    // estandar de .NET para leer configuracion (appsettings.json, variables de
    // entorno, user-secrets, etc.) sin acoplarse a NINGUNA fuente concreta.
    // TokenService no sabe (ni le importa) si "AppSettings:Token" viene de un
    // archivo JSON local o de un secreto inyectado por variables de entorno en
    // un servidor de produccion: solo conoce la interfaz.
    //
    // Notar que esto sigue siendo un servicio UTILITARIO PURO: IConfiguration
    // no es una dependencia de base de datos ni de un repositorio, es
    // simplemente la forma estandar de leer configuracion en .NET.
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // CrearToken: arma y firma un JWT para el Usuario recibido.
    public string CrearToken(Usuario usuario)
    {
        try
        {
            // --- Paso 1: armar los CLAIMS (afirmaciones sobre el usuario) ---
            // Un Claim es un par clave-valor que "afirma" algo sobre el
            // titular del token. Usamos los tipos de ClaimTypes ESTANDAR del
            // ecosistema .NET (en vez de strings arbitrarios) porque son los
            // mismos que ASP.NET Core busca automaticamente al popular
            // "User.Identity" en un controller protegido con [Authorize]: esto
            // evita tener que escribir codigo de mapeo manual mas adelante.
            var claims = new List<Claim>
            {
                // NameIdentifier: el identificador UNICO del usuario (su Id de
                // base de datos). Es el Claim que, en una futura capa Api,
                // permitiria responder preguntas como "¿este usuario logueado
                // es dueño de este ActivoDigital que esta pidiendo borrar?".
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),

                // Email: se incluye como Claim basico pedido por la rubrica.
                // Es informacion NO sensible (a diferencia de PasswordHash),
                // por lo que es seguro que viaje dentro del payload del token
                // (recordemos: el payload es legible, no esta encriptado).
                new Claim(ClaimTypes.Email, usuario.Email),

                // Name: Claim adicional con el nombre del usuario, util para
                // mostrarlo en una futura UI sin tener que hacer una consulta
                // extra a la base de datos solo para saber "como se llama el
                // usuario logueado".
                new Claim(ClaimTypes.Name, usuario.Nombre),

                // Role: el Claim que hace posible el atributo
                // [Authorize(Roles = "Administrador")] en los controllers.
                // ASP.NET Core, al validar el JWT (Program.cs), reconoce
                // automaticamente los Claims de tipo ClaimTypes.Role como los
                // "roles" del usuario autenticado: no hace falta configurar
                // nada adicional para que [Authorize(Roles = "...")] los
                // encuentre, siempre y cuando se use el mismo ClaimTypes.Role
                // (y no un string arbitrario) tanto aca como al leerlo.
                new Claim(ClaimTypes.Role, usuario.Rol.ToString())
            };

            // --- Paso 2: leer la clave secreta del servidor ---
            // "AppSettings:Token" es la CLAVE SIMETRICA con la que el servidor
            // firma (y luego valida) todos los tokens que emite. Es secreta:
            // solo debe existir en la configuracion del SERVIDOR (variables de
            // entorno o user-secrets en un entorno real de produccion, nunca
            // committeada en texto plano a un repositorio publico) y jamas se
            // envia al cliente. Si no esta configurada, no podemos firmar
            // ningun token: se aborta el proceso con un mensaje seguro.
            var claveSecreta = _configuration["AppSettings:Token"];

            if (string.IsNullOrWhiteSpace(claveSecreta))
            {
                throw new AutenticacionException(
                    "No se pudo generar el token de autenticacion: falta configuracion del servidor.");
            }

            // SymmetricSecurityKey envuelve la clave secreta (convertida a
            // bytes UTF8) en el tipo que espera la libreria de JWT.
            // "Simetrica" significa que la MISMA clave se usa tanto para
            // FIRMAR el token (aca) como para VALIDAR su firma mas adelante
            // (en el middleware de autenticacion de la Api): a diferencia de
            // la criptografia asimetrica (RSA), no hay un par publico/privado.
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveSecreta));

            // SigningCredentials combina la clave con el ALGORITMO de firma a
            // usar: HmacSha512Signature, coherente con el resto del sistema
            // (que ya usa HMACSHA512 para las contrasenas), y con un nivel de
            // seguridad mas que adecuado para la firma de tokens.
            var credenciales = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            // --- Paso 3: armar el SecurityTokenDescriptor ---
            // El SecurityTokenDescriptor es el "plano" (blueprint) que le
            // decimos a JwtSecurityTokenHandler que use para construir el
            // token: que Claims incluir (Subject), cuando expira, y con que
            // credenciales firmarlo.
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                // ClaimsIdentity envuelve la lista de Claims armada arriba.
                Subject = new ClaimsIdentity(claims),

                // --- Fecha de expiracion ---
                // Un JWT SIEMPRE debe tener una expiracion razonable (Claim
                // estandar "exp"): si un token fuera valido para siempre, y
                // llegara a ser robado (ej: interceptado en una red insegura),
                // el atacante tendria acceso indefinido a la cuenta de la
                // victima. 30 minutos acota la "ventana de ataque" util de un
                // token robado a algo mucho mas chico que las 2 horas
                // anteriores, a costa de que el cliente deba loguearse de
                // nuevo con mas frecuencia (esta Api todavia no implementa
                // refresh tokens, que permitirian renovar la sesion sin pedir
                // credenciales de nuevo; queda fuera del alcance de esta etapa).
                Expires = DateTime.UtcNow.AddMinutes(30),

                // SigningCredentials: la firma que protege la integridad del
                // token (ver la explicacion de la SIGNATURE al inicio de la
                // clase).
                SigningCredentials = credenciales
            };

            // --- Paso 4: crear y serializar el token ---
            // JwtSecurityTokenHandler es la clase de la libreria
            // System.IdentityModel.Tokens.Jwt que sabe como transformar el
            // SecurityTokenDescriptor en un objeto token real (CreateToken) y,
            // luego, en el string compacto "header.payload.signature" que
            // efectivamente se le entrega al cliente (WriteToken).
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
        catch (AutenticacionException)
        {
            // Ya es una AutenticacionException "amigable" y especifica (por
            // ejemplo, la de clave secreta faltante lanzada arriba): se
            // relanza tal cual, sin volver a envolverla.
            throw;
        }
        catch (Exception ex)
        {
            // Cualquier otro error inesperado (ej: una excepcion interna de la
            // libreria de JWT al procesar la clave o los claims) se traduce a
            // un mensaje generico y seguro. NUNCA se expone el mensaje tecnico
            // real ni el StackTrace al cliente: podria revelar detalles de la
            // configuracion de seguridad del servidor. El detalle original
            // queda preservado unicamente en "ex", como InnerException, para
            // diagnostico interno del lado del servidor.
            throw new AutenticacionException("Ocurrio un error al generar el token de autenticacion.", ex);
        }
    }
}
