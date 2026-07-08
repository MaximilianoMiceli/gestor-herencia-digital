using Herencia.Business.Dtos;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Herencia.Data.Models;
using Microsoft.AspNetCore.Mvc;

namespace Herencia.Api.Controllers;

// AuthController expone los dos endpoints publicos de AUTENTICACION:
// registro (alta de una cuenta nueva) y login (intercambiar credenciales por
// un Token JWT). A diferencia de ActivosDigitalesController, este controller
// NO lleva [Authorize]: seria contradictorio exigir un Token JWT para poder
// obtener el PRIMER Token JWT.
//
// --- Arquitectura: por que se orquestan 3 interfaces aca, en el controller ---
// Este controller inyecta IUsuarioService (para buscar/crear el Usuario),
// ISeguridadService (para verificar la contrasena) y ITokenService (para
// emitir el JWT). La orquestacion PUNTUAL de "primero verifico la contrasena,
// despues genero el token" vive aca porque es, literalmente, el trabajo de
// autenticacion HTTP (decidir si esta request se autentica y con que
// credenciales), mientras que cada interfaz inyectada sigue encapsulando su
// propia responsabilidad de negocio/seguridad por separado (ninguna de las
// tres sabe nada de las otras dos). Ninguna de las interfaces inyectadas es un
// repositorio ni el AppDbContext: la base de datos sigue completamente
// aislada detras de la capa Business.
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;
    private readonly ISeguridadService _seguridadService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUsuarioService usuarioService,
        ISeguridadService seguridadService,
        ITokenService tokenService,
        ILogger<AuthController> logger)
    {
        _usuarioService = usuarioService;
        _seguridadService = seguridadService;
        _tokenService = tokenService;
        _logger = logger;
    }

    // POST api/auth/registro
    //
    // Verbo POST: crea una cuenta de Usuario nueva. Es, en esencia, el mismo
    // caso de uso que "POST /api/usuarios" (UsuariosController.Crear), pero
    // expuesto bajo el namespace publico de autenticacion, con su propio DTO
    // (RegistroDTO) para documentar la intencion de "auto-registro".
    [HttpPost("registro")]
    public async Task<ActionResult<UsuarioDTO>> Registro(RegistroDTO registroDTO)
    {
        // Con [ApiController] (heredado implicitamente en toda la Api) el
        // ModelState ya se valido automaticamente contra los [Required] de
        // RegistroDTO/UsuarioCreacionDTO antes de llegar aca. Se deja este
        // chequeo explicito para que quede documentado en el propio codigo
        // del endpoint el motivo del 400 ante datos estructuralmente
        // invalidos (nombre/email/password vacios).
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Delegamos TODA la logica de negocio (validar formato de email,
            // largo minimo de contrasena, calcular hash/salt via
            // ISeguridadService, persistir) al servicio: CrearUsuarioAsync
            // hace exactamente lo mismo que ya usa UsuariosController.Crear.
            var usuarioCreado = await _usuarioService.CrearUsuarioAsync(registroDTO);

            // 201 Created: se creo exitosamente el nuevo Usuario. El header
            // "Location" de la respuesta apunta a "GET api/usuarios/{id}",
            // que vive en OTRO controller (UsuariosController): CreatedAtAction
            // permite indicar el nombre del ACTION ("ObtenerPorId") y el
            // nombre del CONTROLLER ("Usuarios", sin el sufijo "Controller")
            // de forma explicita quando la ruta de consulta no pertenece al
            // controller actual.
            return CreatedAtAction(
                actionName: nameof(UsuariosController.ObtenerPorId),
                controllerName: "Usuarios",
                routeValues: new { id = usuarioCreado.Id },
                value: usuarioCreado);
        }
        catch (ReglaNegocioException ex)
        {
            // Nombre vacio, email con formato invalido, contrasena demasiado
            // corta, email ya registrado (constraint UNIQUE de la base de
            // datos), etc.: todos estos casos ya llegan envueltos en
            // ReglaNegocioException desde UsuarioService. 400 Bad Request:
            // el problema es responsabilidad del cliente (datos invalidos).
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al registrar un nuevo usuario.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    // POST api/auth/login
    //
    // Verbo POST: NO crea ni modifica ningun recurso persistente (no hay un
    // "recurso Login" que perdure en la base de datos); se usa POST y no GET
    // porque las credenciales (sobre todo la contrasena) viajan en el BODY de
    // la request, nunca en la URL o el query string, donde quedarian
    // expuestas en logs de servidores intermedios, historiales del navegador,
    // o cacheadas por proxies.
    [HttpPost("login")]
    public async Task<ActionResult<TokenRespuestaDTO>> Login(LoginDTO loginDTO)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            UsuarioAutenticacionDTO usuarioAutenticacion;

            try
            {
                // Paso 1: ¿el usuario existe? Se busca por Email (el dato con
                // el que el cliente se identifica) y se obtiene el
                // UsuarioAutenticacionDTO, el UNICO DTO de la Api que incluye
                // PasswordHash/PasswordSalt (ver su comentario para el detalle
                // de por que es seguro que este DTO puntual los incluya).
                usuarioAutenticacion = await _usuarioService.ObtenerUsuarioParaAutenticacionAsync(loginDTO.Email);
            }
            catch (RecursoNoEncontradoException)
            {
                // --- SEGURIDAD: por que NO se devuelve un mensaje distinto aca? ---
                // Si respondieramos algo como "el email no esta registrado"
                // en este catch, y "la contrasena es incorrecta" mas abajo en
                // el paso 2, un atacante podria usar ese endpoint para hacer
                // "user enumeration": probar miles de emails y, por la
                // diferencia de mensaje, deducir CUALES de esos emails
                // corresponden a cuentas reales del sistema (sin siquiera
                // necesitar la contrasena correcta). Por eso, tanto "el email
                // no existe" como "la contrasena esta mal" (paso 2) devuelven
                // EXACTAMENTE el mismo codigo (401) y el mismo mensaje
                // generico "Credenciales invalidas.": desde afuera, ambos
                // casos son indistinguibles.
                return Unauthorized(new { mensaje = "Credenciales invalidas." });
            }

            // Paso 2: ¿la contrasena ingresada es correcta? Se recalcula el
            // hash de "loginDTO.Password" usando el MISMO salt persistido
            // (usuarioAutenticacion.PasswordSalt) y se compara, de forma seria
            // (tiempo constante, ver SeguridadService.VerificarPasswordHash),
            // contra el hash guardado (usuarioAutenticacion.PasswordHash).
            var credencialesValidas = _seguridadService.VerificarPasswordHash(
                loginDTO.Password,
                usuarioAutenticacion.PasswordHash,
                usuarioAutenticacion.PasswordSalt);

            if (!credencialesValidas)
            {
                // Mismo codigo Y mismo mensaje que el catch de arriba: ver el
                // comentario sobre "user enumeration" mas arriba.
                return Unauthorized(new { mensaje = "Credenciales invalidas." });
            }

            // Paso 3: credenciales correctas -> emitir el Token JWT. Se arma
            // un objeto "Usuario" TRANSITORIO (nunca se persiste, nunca se
            // devuelve en la respuesta HTTP) solo para pasarle a
            // ITokenService.CrearToken los datos que necesita (Id, Nombre,
            // Email, Rol): es simplemente un "contenedor de datos" en
            // memoria para cumplir la firma de ese servicio, no una fuga de
            // la entidad de dominio hacia el cliente. El Rol viaja tal cual
            // esta persistido en este instante (no es un valor fijo), para
            // que el token refleje el nivel de permisos REAL y actualizado
            // del usuario.
            var usuarioParaToken = new Usuario
            {
                Id = usuarioAutenticacion.Id,
                Nombre = usuarioAutenticacion.Nombre,
                Email = usuarioAutenticacion.Email,
                Rol = usuarioAutenticacion.Rol
            };

            var token = _tokenService.CrearToken(usuarioParaToken);

            // 200 OK: el login se completo con exito. Se devuelve el Token
            // JWT en el body de la respuesta; a partir de aca, el cliente
            // debe reenviar este token en el header "Authorization: Bearer
            // <token>" de cada request a un endpoint protegido con
            // [Authorize] (ej: GET /api/activos).
            return Ok(new TokenRespuestaDTO { Token = token });
        }
        catch (AutenticacionException ex)
        {
            // TokenService.CrearToken puede fallar (ej: falta la clave
            // "AppSettings:Token" en la configuracion del servidor). Esto NO
            // es responsabilidad del cliente (sus credenciales eran
            // correctas): es un problema de configuracion/infraestructura del
            // SERVIDOR, por lo que corresponde 500, no 401 ni 400. El mensaje
            // de AutenticacionException ya esta pensado para ser seguro de
            // mostrar (no expone la clave real ni detalles tecnicos).
            _logger.LogError(ex, "Error al generar el token de autenticacion.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado durante el login.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }
}
