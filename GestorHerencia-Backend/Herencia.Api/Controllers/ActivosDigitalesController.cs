using System.Security.Claims;
using Herencia.Business.Dtos;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Herencia.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Herencia.Api.Controllers;

// ActivosDigitalesController expone el recurso "ActivoDigital" por HTTP.
// Igual que UsuariosController, es una capa DELGADA: no contiene ninguna
// regla de negocio (esa vive en ActivoDigitalService), solo traduce HTTP <->
// llamadas al servicio. El listado de activos de un usuario puntual (sin
// paginar) vive como ruta anidada en UsuariosController
// ("GET api/usuarios/{id}/activos"), por eso no se repite aca.
//
// --- ¿Por que [Authorize] a nivel de CLASE? ---
// Un ActivoDigital es informacion PERSONAL y sensible (cuentas bancarias,
// billeteras cripto, correos, redes sociales de una persona): ningun endpoint
// de este controller deberia poder invocarse sin antes demostrar "quien sos"
// mediante un Token JWT valido. Poner [Authorize] a nivel de clase (en vez de
// repetirlo endpoint por endpoint) protege TODOS los actions por defecto,
// incluidos los que se agreguen en el futuro: es un modelo de seguridad
// "seguro por defecto" (secure by default), donde hay que decidir
// explicitamente (con [AllowAnonymous]) si algun endpoint puntual necesita
// quedar publico, en vez de arriesgarse a olvidar proteger uno nuevo.
//
// Tecnicamente, [Authorize] le dice al MIDDLEWARE de Authorization (agregado
// en Program.cs con app.UseAuthorization()) que, antes de ejecutar CUALQUIER
// action de este controller, verifique que el HttpContext.User actual este
// autenticado (es decir, que el middleware de Authentication, en el paso
// anterior del pipeline, haya podido leer y validar un JWT valido del header
// "Authorization: Bearer <token>"). Si no hay token, o el token es invalido/
// expirado/mal firmado, el request se corta ACA MISMO con un 401 Unauthorized
// automatico, sin que el codigo de ningun action de este controller llegue
// siquiera a ejecutarse.
[ApiController]
[Authorize]
[Route("api/activosdigitales")]
public class ActivosDigitalesController : ControllerBase
{
    // Se inyecta UNICAMENTE la interfaz de servicio (IActivoDigitalService),
    // nunca IActivoDigitalRepository ni AppDbContext. Esto mantiene a la base
    // de datos completamente aislada detras de la capa Business: si mañana
    // cambia el motor de base de datos o el ORM, este controller no se entera
    // ni necesita cambiar una sola linea.
    private readonly IActivoDigitalService _activoDigitalService;

    // Se inyecta ademas IAsignacionHerenciaService (nunca su repositorio) para
    // exponer, anidada bajo este mismo recurso, la relacion maestro-detalle
    // "ActivoDigital (maestro) -> AsignacionHerencia (detalle)": ver
    // ObtenerAsignaciones/CrearAsignaciones mas abajo.
    private readonly IAsignacionHerenciaService _asignacionHerenciaService;

    private readonly ILogger<ActivosDigitalesController> _logger;

    public ActivosDigitalesController(
        IActivoDigitalService activoDigitalService,
        IAsignacionHerenciaService asignacionHerenciaService,
        ILogger<ActivosDigitalesController> logger)
    {
        _activoDigitalService = activoDigitalService;
        _asignacionHerenciaService = asignacionHerenciaService;
        _logger = logger;
    }

    // --- Helper privado: extraer el Id del usuario autenticado del Token JWT ---
    // Centraliza en un unico lugar la logica de leer el Claim
    // ClaimTypes.NameIdentifier (ver el comentario detallado en
    // ObtenerMisActivosPaginado, mas abajo, para el detalle de POR QUE este
    // valor es confiable). Devuelve "int?" (null si el Claim faltara o no
    // fuera numerico): un caso defensivo que, en la practica, no deberia
    // ocurrir con tokens emitidos por esta misma Api, pero que evita asumir
    // ciegamente la forma del token en CADA action que necesita este dato.
    private int? ObtenerUsuarioIdAutenticado()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return (claim is not null && int.TryParse(claim.Value, out var usuarioId)) ? usuarioId : null;
    }

    // GET /api/activos?pagina=1&limite=10&tipo=CuentaBancaria&nombre=santander
    //
    // Nota de ruteo: se usa "~/api/activos" (el "~" al inicio IGNORA el
    // prefijo de ruta del controller, "api/activosdigitales", y fuerza la
    // ruta ABSOLUTA pedida explicitamente por la rubrica) en vez de que este
    // endpoint quede en "api/activosdigitales/api/activos" o similar.
    //
    // Este endpoint es el listado "propio" del usuario autenticado: NO recibe
    // ningun Id de usuario ni en la ruta ni en el query string, JUSTAMENTE
    // para que sea imposible pedir los activos de OTRO usuario cambiando un
    // numero en la URL (ver el comentario sobre ClaimTypes.NameIdentifier mas
    // abajo). Sigue siendo un GET porque es una operacion de solo lectura.
    //
    // --- Filtros "tipo" y "nombre" (busqueda por MULTIPLES parametros) ---
    // Ambos son OPCIONALES ([FromQuery] con valor default null): el cliente
    // puede combinarlos libremente con la paginacion, ej:
    // "GET /api/activos?tipo=CuentaBancaria&nombre=santander&pagina=1&limite=10"
    // para buscar, entre los activos del usuario autenticado, solo los que
    // son cuentas bancarias Y cuyo nombre contenga "santander". Si no se
    // envia ninguno de los dos, el comportamiento es identico al listado sin
    // filtrar (ver ActivoDigitalRepository.ObtenerActivosPorUsuarioPaginadoAsync).
    [HttpGet("~/api/activos")]
    public async Task<ActionResult<ResultadoPaginadoDTO<ActivoDigitalDTO>>> ObtenerMisActivosPaginado(
        [FromQuery] int pagina = 1,
        [FromQuery] int limite = 10,
        [FromQuery] TipoActivoDigital? tipo = null,
        [FromQuery] string? nombre = null)
    {
        try
        {
            // --- ¿Como se extrae el Id del usuario autenticado del Token JWT? ---
            // Gracias al middleware de Authentication (Program.cs), para
            // cuando el codigo de este action se ejecuta, ASP.NET Core ya
            // valido el JWT recibido y armo un "ClaimsPrincipal" con TODOS
            // los Claims que TokenService.CrearToken empaqueto originalmente
            // dentro del token (ver esa clase). Ese ClaimsPrincipal queda
            // disponible en la propiedad "User" de todo ControllerBase, y
            // ObtenerUsuarioIdAutenticado() (ver mas arriba) busca dentro de
            // esos Claims el que se agrego con
            // "new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString())"
            // al crear el token.
            //
            // Este valor NO puede ser falsificado por el cliente sin invalidar
            // la firma del token (ver TokenService): un atacante no puede,
            // por ejemplo, editar a mano el payload de un JWT ajeno para
            // cambiar el Id y hacerse pasar por otro usuario, porque la firma
            // HMAC-SHA512 dejaria de coincidir y el middleware de
            // Authentication rechazaria el token ANTES de llegar aca. Esta es
            // la garantia real de seguridad: "solo puede ver sus propios
            // activos" no es un chequeo que hagamos nosotros comparando ids,
            // sino una consecuencia directa de que el usuarioId sale de un
            // token criptograficamente verificado, nunca de un dato que el
            // cliente puede escribir libremente (como si fuera un parametro
            // de la URL).
            var usuarioId = ObtenerUsuarioIdAutenticado();

            if (usuarioId is null)
            {
                // Caso extremo/defensivo: un token tecnicamente valido (bien
                // firmado, no expirado) pero al que, por algun motivo, le
                // falta el Claim de identificacion o tiene un valor no
                // numerico. No deberia ocurrir con tokens emitidos por
                // ESTA Api (TokenService siempre agrega este Claim), pero se
                // contempla para no asumir ciegamente la forma del token.
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            // Delegamos TODA la logica de paginacion y filtrado (Skip/Take,
            // normalizar "pagina"/"limite", filtrar por tipo/nombre, contar
            // el total) al servicio de Business. El controller no sabe (ni
            // le importa) como se arma la consulta SQL subyacente: solo pasa
            // el usuarioId (ya verificado via JWT) y los parametros recibidos
            // por query string.
            var resultado = await _activoDigitalService.ObtenerActivosPorUsuarioPaginadoAsync(usuarioId.Value, pagina, limite, tipo, nombre);

            // 200 OK: la consulta se resolvio correctamente, incluso si la
            // pagina pedida resulta vacia (ej: se pidio la pagina 50 y el
            // usuario solo tiene 2 paginas de resultados): seguir siendo una
            // respuesta valida, no un error.
            return Ok(resultado);
        }
        catch (RecursoNoEncontradoException ex)
        {
            // Solo podria ocurrir si el usuario del token fue eliminado de la
            // base de datos DESPUES de haberse emitido el token (el token en
            // si mismo seguiria siendo valido hasta su expiracion, pero el
            // usuario ya no existe). Se traduce a 404 Not Found.
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al obtener los activos paginados del usuario autenticado.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    // GET api/activosdigitales/{id}
    //
    // Verbo GET: operacion de solo lectura para pedir UN ActivoDigital
    // puntual por su Id.
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ActivoDigitalDTO>> ObtenerPorId(int id)
    {
        try
        {
            var activoDigital = await _activoDigitalService.ObtenerActivoDigitalPorIdAsync(id);

            // --- Verificacion de OWNERSHIP (IDOR) ---
            // [Authorize] (a nivel de clase) solo garantiza que el request
            // trae un Token JWT VALIDO de ALGUN usuario: no garantiza, por si
            // solo, que ese usuario sea el DUEÑO del ActivoDigital puntual
            // que se esta pidiendo por Id. Sin este chequeo adicional, CUALQUIER
            // usuario autenticado podria enumerar Ids (1, 2, 3, ...) y leer
            // los activos digitales de OTROS usuarios: esto se conoce como
            // IDOR (Insecure Direct Object Reference), una de las
            // vulnerabilidades mas comunes en Apis REST. Por eso se compara
            // el UsuarioId del activo devuelto por el servicio contra el Id
            // del usuario autenticado (extraido del Claim del JWT).
            var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

            if (usuarioAutenticadoId is null)
            {
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            if (activoDigital.UsuarioId != usuarioAutenticadoId)
            {
                // 403 Forbidden: a diferencia de 401 (no autenticado), aca el
                // usuario SI demostro "quien es" con un token valido, pero ese
                // "quien es" no tiene permiso sobre ESTE recurso puntual (no
                // es su dueño). No se revela informacion adicional sobre el
                // activo ajeno (ni siquiera si realmente existe otro dueño):
                // el mensaje es generico a proposito.
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para acceder a este activo digital." });
            }

            // 200 OK: se encontro el activo solicitado Y pertenece al usuario
            // autenticado.
            return Ok(activoDigital);
        }
        catch (RecursoNoEncontradoException ex)
        {
            // El Id no corresponde a ningun ActivoDigital existente: 404 Not
            // Found, con el mensaje amigable que ya arma el servicio.
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al obtener el activo digital con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    // POST api/activosdigitales
    //
    // Verbo POST: crea un nuevo ActivoDigital.
    [HttpPost]
    public async Task<ActionResult<ActivoDigitalDTO>> Crear(ActivoDigitalCreacionDTO activoDigitalCreacionDTO)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

        if (usuarioAutenticadoId is null)
        {
            return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
        }

        // --- Nunca confiar en el UsuarioId que venga en el BODY ---
        // ActivoDigitalCreacionDTO.UsuarioId existe en el DTO (para que el
        // servicio de Business sepa a que titular asociar el nuevo activo),
        // pero ahora que el endpoint esta autenticado, ya SABEMOS con certeza
        // quien es el usuario que esta creando el recurso: es quien sea que
        // el Token JWT valido diga que es. Sobreescribir el campo aca, ANTES
        // de llamar al servicio, hace fisicamente imposible que un usuario
        // autenticado cree un activo a nombre de OTRO usuario mandando un
        // UsuarioId distinto al suyo en el body (otro caso de IDOR, esta vez
        // en la creacion en vez de en la lectura).
        activoDigitalCreacionDTO.UsuarioId = usuarioAutenticadoId.Value;

        try
        {
            var activoCreado = await _activoDigitalService.CrearActivoDigitalAsync(activoDigitalCreacionDTO);

            // 201 Created + header "Location" apuntando a "GET
            // api/activosdigitales/{id}" (esta misma action, ObtenerPorId),
            // tal como exige el estandar REST para toda creacion exitosa.
            return CreatedAtAction(
                nameof(ObtenerPorId),
                new { id = activoCreado.Id },
                activoCreado);
        }
        catch (RecursoNoEncontradoException ex)
        {
            // Caso particular de este endpoint: el servicio puede lanzar
            // RecursoNoEncontradoException (no solo ReglaNegocioException) si
            // el UsuarioId indicado en el body no corresponde a ningun
            // Usuario existente. Semanticamente sigue siendo "el cliente
            // referencio un recurso relacionado que no existe", asi que
            // tambien se traduce a 404 Not Found (y no a 400), para ser
            // coherentes con el resto de la Api.
            return NotFound(new { mensaje = ex.Message });
        }
        catch (ReglaNegocioException ex)
        {
            // El Nombre vino vacio, o cualquier otra regla de negocio de
            // formato: 400 Bad Request, responsabilidad del cliente.
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al crear un activo digital.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    // PUT api/activosdigitales/{id}
    //
    // Verbo PUT: actualiza Nombre, Tipo y Descripcion de un ActivoDigital
    // EXISTENTE (identificado por el Id de la URL). Es idempotente: repetir
    // la misma request dos veces deja el activo en el mismo estado final.
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ActivoDigitalDTO>> Actualizar(int id, ActivoDigitalActualizacionDTO activoDigitalActualizacionDTO)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

            if (usuarioAutenticadoId is null)
            {
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            // --- Verificacion de OWNERSHIP antes de escribir ---
            // Se busca el activo EXISTENTE primero (una lectura extra, antes
            // del UPDATE) unicamente para conocer su UsuarioId real y poder
            // compararlo contra el usuario autenticado. Sin este chequeo,
            // cualquier usuario logueado podria modificar Nombre/Tipo/
            // Descripcion de un ActivoDigital ajeno con solo adivinar su Id.
            var activoExistente = await _activoDigitalService.ObtenerActivoDigitalPorIdAsync(id);

            if (activoExistente.UsuarioId != usuarioAutenticadoId)
            {
                // 403 Forbidden: token valido, pero sin permiso sobre ESTE
                // recurso puntual (ver el mismo razonamiento en ObtenerPorId).
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para modificar este activo digital." });
            }

            var activoActualizado = await _activoDigitalService.ActualizarActivoDigitalAsync(id, activoDigitalActualizacionDTO);

            // 200 OK: se devuelve el activo ya actualizado.
            return Ok(activoActualizado);
        }
        catch (RecursoNoEncontradoException ex)
        {
            // El Id de la URL no existe: no hay nada que actualizar.
            return NotFound(new { mensaje = ex.Message });
        }
        catch (ReglaNegocioException ex)
        {
            // Nombre vacio u otra regla de negocio violada por los nuevos
            // datos: 400 Bad Request.
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al actualizar el activo digital con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    // DELETE api/activosdigitales/{id}
    //
    // Verbo DELETE: elimina el ActivoDigital identificado por el Id de la URL.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

            if (usuarioAutenticadoId is null)
            {
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            // Misma verificacion de OWNERSHIP que en Actualizar: se confirma
            // que el activo pertenece al usuario autenticado ANTES de
            // borrarlo, para que nadie pueda eliminar activos ajenos
            // adivinando su Id.
            var activoExistente = await _activoDigitalService.ObtenerActivoDigitalPorIdAsync(id);

            if (activoExistente.UsuarioId != usuarioAutenticadoId)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para eliminar este activo digital." });
            }

            await _activoDigitalService.EliminarActivoDigitalAsync(id);

            // 204 No Content: borrado exitoso, sin cuerpo de respuesta (ya no
            // hay ningun recurso que representar).
            return NoContent();
        }
        catch (RecursoNoEncontradoException ex)
        {
            // El Id no existe: no hay nada que borrar. 404 Not Found.
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al eliminar el activo digital con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    // GET api/activosdigitales/{id}/asignaciones
    //
    // Ruta anidada MAESTRO-DETALLE: el "maestro" es el ActivoDigital
    // identificado por "{id}", y el "detalle" son sus AsignacionHerencia (a
    // que Usuario beneficiario, con que porcentaje, bajo que condicion se
    // reparte). Sigue siendo un GET porque es una consulta de solo lectura.
    [HttpGet("{id:int}/asignaciones")]
    public async Task<ActionResult<IEnumerable<AsignacionHerenciaDTO>>> ObtenerAsignaciones(int id)
    {
        try
        {
            var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

            if (usuarioAutenticadoId is null)
            {
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            // Verificacion de OWNERSHIP: el activo (el "maestro") debe
            // pertenecer al usuario autenticado antes de mostrarle su detalle.
            var activoDigital = await _activoDigitalService.ObtenerActivoDigitalPorIdAsync(id);

            if (activoDigital.UsuarioId != usuarioAutenticadoId)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para acceder a las asignaciones de este activo digital." });
            }

            var asignaciones = await _asignacionHerenciaService.ObtenerAsignacionesPorActivoAsync(id);

            return Ok(asignaciones);
        }
        catch (RecursoNoEncontradoException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al obtener las asignaciones del activo digital con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    // POST api/activosdigitales/{id}/asignaciones
    //
    // Verbo POST: crea un LOTE de asignaciones (reparte el ActivoDigital
    // "{id}" entre uno o mas beneficiarios, invitados por Email) en una
    // UNICA operacion atomica.
    //
    // --- Transacciones y reversion ante error ---
    // El body es una LISTA de AsignacionHerenciaCreacionDTO (ej: "repartir
    // este activo 50% para un hijo, 50% para otra hija" en una sola llamada).
    // IAsignacionHerenciaService.CrearAsignacionesAsync procesa TODO el lote
    // dentro de una unica transaccion de base de datos (ver el detalle
    // completo en AsignacionHerenciaService y RepositorioBase.EjecutarEnTransaccionAsync):
    // si CUALQUIER elemento del lote resulta invalido (el otorgante
    // intentando asignarse el activo a si mismo, o un porcentaje que hace
    // superar el 100% del activo), la transaccion completa se REVIERTE y
    // NINGUNA asignacion del lote queda persistida, ni siquiera las que se
    // hubieran procesado con exito antes de la que fallo.
    [HttpPost("{id:int}/asignaciones")]
    public async Task<ActionResult<IEnumerable<AsignacionHerenciaDTO>>> CrearAsignaciones(
        int id,
        List<AsignacionHerenciaCreacionDTO> asignacionesCreacionDTO)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

            if (usuarioAutenticadoId is null)
            {
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            // Verificacion de OWNERSHIP: solo el titular del ActivoDigital
            // puede repartirlo (la regla de negocio de
            // AsignacionHerenciaService, mas abajo, refuerza ademas que el
            // otorgante no pueda asignarse el activo a si mismo).
            var activoDigital = await _activoDigitalService.ObtenerActivoDigitalPorIdAsync(id);

            if (activoDigital.UsuarioId != usuarioAutenticadoId)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para repartir este activo digital." });
            }

            var asignacionesCreadas = (await _asignacionHerenciaService.CrearAsignacionesAsync(id, asignacionesCreacionDTO)).ToList();

            // --- Notificacion simulada (por consola) al/a los beneficiarios ---
            // En un sistema real esto seria un correo (SMTP/SendGrid/etc.) o
            // una notificacion push; aca se simula por consola para poder
            // demostrar el flujo completo sin depender de infraestructura
            // externa. El mensaje difiere segun el resultado de
            // CrearAsignacionesAsync para cada fila:
            //  - UsuarioBeneficiarioId con valor: la persona YA tenia cuenta,
            //    asi que alcanza con una notificacion IN-APP (la va a ver la
            //    proxima vez que abra "mis herencias").
            //  - UsuarioBeneficiarioId null: la persona NO tiene cuenta
            //    todavia, asi que se le simula un email invitandola a
            //    crearse una para poder reclamar la herencia.
            foreach (var asignacion in asignacionesCreadas)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine();
                Console.WriteLine("======================================================================");

                if (asignacion.UsuarioBeneficiarioId is null)
                {
                    // Se usa TokenInvitacion (no "asignacion.Id") en la URL:
                    // este link se le entrega a alguien de afuera del
                    // sistema, por Email, y los dos endpoints publicos que
                    // lo consumen (InvitacionesController) no verifican
                    // ownership por JWT. Usar el Id secuencial permitiria
                    // enumerar invitaciones ajenas con solo cambiar un
                    // numero en la URL (ver el comentario detallado en
                    // AsignacionHerencia.TokenInvitacion).
                    var link = $"http://localhost:8081/invitacion?token={asignacion.TokenInvitacion}";
                    Console.WriteLine($"[EMAIL SIMULADO] Invitacion a crear cuenta enviada a: {asignacion.EmailInvitado}");
                    Console.WriteLine("Para reclamar esta herencia, primero cree una cuenta con este mismo email:");
                    Console.WriteLine($"👉 {link}");
                }
                else
                {
                    Console.WriteLine($"[NOTIFICACION IN-APP SIMULADA] {asignacion.EmailInvitado} ya tiene cuenta.");
                    Console.WriteLine($"Se le notifico una nueva herencia pendiente (AsignacionId={asignacion.Id}) en su proxima sesion.");
                }

                Console.WriteLine("======================================================================");
                Console.WriteLine();
                Console.ResetColor();
            }

            // 201 Created: se creo exitosamente el lote completo de
            // asignaciones. Se apunta el header "Location" hacia el listado
            // de asignaciones del activo (no hay un unico Id "principal"
            // cuando se crean varias filas a la vez).
            return CreatedAtAction(
                nameof(ObtenerAsignaciones),
                new { id },
                asignacionesCreadas);
        }
        catch (RecursoNoEncontradoException ex)
        {
            // El activo no existe: 404 Not Found.
            return NotFound(new { mensaje = ex.Message });
        }
        catch (ReglaNegocioException ex)
        {
            // Porcentaje invalido, suma superior al 100%, o auto-asignacion:
            // 400 Bad Request, responsabilidad del cliente. Gracias a la
            // transaccion, en este punto la base de datos NO tiene ningun
            // rastro parcial del lote fallido.
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al crear asignaciones para el activo digital con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    // POST api/activosdigitales/{id}/archivo
    //
    // Recibe un formulario multipart (no JSON) con un unico campo "archivo":
    // el documento a adjuntar (PDF/JPG/PNG) a un ActivoDigital que YA existe.
    // Reemplaza cualquier archivo adjunto previo (el nombre en disco siempre
    // es nuevo, ver AlmacenamientoLocalService, asi que el archivo anterior
    // queda huerfano en disco en vez de sobreescribirse: aceptable para el
    // alcance de este proyecto, igual que ya ocurre si CertificadoDefuncionService
    // alguna vez permitiera resubir).
    [HttpPost("{id:int}/archivo")]
    public async Task<ActionResult<ActivoDigitalDTO>> SubirArchivo(int id, [FromForm] IFormFile archivo)
    {
        if (archivo is null || archivo.Length == 0)
        {
            return BadRequest(new { mensaje = "Debe adjuntar un archivo." });
        }

        try
        {
            var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

            if (usuarioAutenticadoId is null)
            {
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            // Misma verificacion de OWNERSHIP que Actualizar/Eliminar: solo
            // el titular del ActivoDigital puede adjuntarle un archivo.
            var activoExistente = await _activoDigitalService.ObtenerActivoDigitalPorIdAsync(id);

            if (activoExistente.UsuarioId != usuarioAutenticadoId)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para adjuntar un archivo a este activo digital." });
            }

            await using var contenido = archivo.OpenReadStream();

            var activoActualizado = await _activoDigitalService.SubirArchivoAsync(
                id, contenido, archivo.FileName, archivo.ContentType, archivo.Length);

            return Ok(activoActualizado);
        }
        catch (RecursoNoEncontradoException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (ReglaNegocioException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al subir el archivo del activo digital con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }
}
