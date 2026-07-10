using System.Security.Claims;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Herencia.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Herencia.Api.Controllers;

// DTO para retornar los datos de la invitacion en un formato limpio para el
// cliente movil (Frame 21: la tarjeta de invitacion).
public class InvitacionDTO
{
    // Se expone el TOKEN (no el Id entero interno) para que la app movil lo
    // reutilice tal cual al llamar a POST /api/invitaciones/{token}/procesar.
    public string Token { get; set; } = string.Empty;
    public string EmisorNombre { get; set; } = string.Empty;
    public string BeneficiarioNombre { get; set; } = string.Empty;
    public string BeneficiarioEmail { get; set; } = string.Empty;
}

// DTO que representa una herencia recibida para el listado de "Mis
// herencias" (Frame 24).
public class MiHerenciaDTO
{
    public int AsignacionId { get; set; }
    public string TitularNombre { get; set; } = string.Empty;
    public string ActivoNombre { get; set; } = string.Empty;
    public int ActivoTipo { get; set; }
    public decimal Porcentaje { get; set; }
    public string CondicionLiberacion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}

// Modelo de datos recibido para procesar la invitacion.
public class ProcesarInvitacionRequest
{
    public string Accion { get; set; } = string.Empty; // "aceptar" o "rechazar"
}

// Controlador publico de invitaciones. Encargado de posibilitar la consulta
// y confirmacion de invitaciones de herencia.
//
// --- ¿Por que ya NO trabaja directo contra AppDbContext? ---
// La version anterior de este controller consultaba "_context.Beneficiarios"
// directamente, saltandose por completo la capa Business (y, con ella, sus
// validaciones y su traduccion de excepciones). Eso violaba la separacion de
// capas que respeta el resto de la Api (ver, por comparacion, cualquier otro
// controller: siempre dependen de una interfaz de Business, nunca de
// AppDbContext). Ahora este controller es tan "delgado" como los demas:
// delega TODA la logica a IAsignacionHerenciaService, IUsuarioService e
// IActivoDigitalService, y solo traduce HTTP <-> llamadas a esos servicios.
//
// --- ¿Por que el modelo de doble rol cambia el significado de "invitacion"? ---
// Con la entidad Beneficiario separada, se podia "invitar" a alguien sin
// asociarlo todavia a ningun activo puntual. Con el modelo de doble rol, la
// invitacion Y la asignacion de un activo puntual son la MISMA operacion
// (ver AsignacionHerenciaService.CrearAsignacionesAsync): invitar a alguien
// por Email es, directamente, crear una AsignacionHerencia con
// UsuarioId en null. Por eso el "Id" de una invitacion, en este controller,
// pasa a ser el TokenInvitacion de esa AsignacionHerencia (un identificador
// PUBLICO no adivinable, generado aleatoriamente; ver el comentario
// detallado en AsignacionHerencia.TokenInvitacion), nunca su Id entero
// autoincremental: estos dos endpoints son PUBLICOS (sin [Authorize]) y no
// verifican ownership por Token JWT, asi que la unica proteccion posible
// contra que alguien enumere invitaciones ajenas es que el identificador en
// si sea imposible de adivinar.
[ApiController]
[Route("api/invitaciones")]
public class InvitacionesController : ControllerBase
{
    private readonly IAsignacionHerenciaService _asignacionHerenciaService;
    private readonly IUsuarioService _usuarioService;
    private readonly IActivoDigitalService _activoDigitalService;
    private readonly ILogger<InvitacionesController> _logger;

    public InvitacionesController(
        IAsignacionHerenciaService asignacionHerenciaService,
        IUsuarioService usuarioService,
        IActivoDigitalService activoDigitalService,
        ILogger<InvitacionesController> logger)
    {
        _asignacionHerenciaService = asignacionHerenciaService;
        _usuarioService = usuarioService;
        _activoDigitalService = activoDigitalService;
        _logger = logger;
    }

    // GET api/invitaciones/{token}
    //
    // Endpoint publico (sin [Authorize]) para que la app cargue los datos de
    // la tarjeta de invitacion (Frame 21) sin requerir que el usuario este
    // logueado todavia: el "token" es AsignacionHerencia.TokenInvitacion
    // (ver el comentario de la clase).
    [HttpGet("{token}")]
    public async Task<ActionResult<InvitacionDTO>> ObtenerInvitacion(string token)
    {
        try
        {
            var asignacion = await _asignacionHerenciaService.ObtenerAsignacionPorTokenAsync(token);

            var emisor = await _usuarioService.ObtenerUsuarioPorIdAsync(asignacion.UsuarioOtorganteId);

            // El beneficiario solo tiene "Nombre" si ya reclamo la
            // invitacion con una cuenta propia (UsuarioBeneficiarioId no
            // nulo); si todavia no se registro, solo se conoce su Email.
            var beneficiarioNombre = string.Empty;

            if (asignacion.UsuarioBeneficiarioId is not null)
            {
                var beneficiario = await _usuarioService.ObtenerUsuarioPorIdAsync(asignacion.UsuarioBeneficiarioId.Value);
                beneficiarioNombre = beneficiario.Nombre;
            }

            var dto = new InvitacionDTO
            {
                Token = asignacion.TokenInvitacion,
                EmisorNombre = emisor.Nombre,
                BeneficiarioNombre = beneficiarioNombre,
                BeneficiarioEmail = asignacion.EmailInvitado
            };

            return Ok(dto);
        }
        catch (RecursoNoEncontradoException)
        {
            return NotFound(new { mensaje = "La invitacion no existe o ha sido revocada." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al obtener la invitacion con token {Token}.", token);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    // POST api/invitaciones/{token}/procesar
    //
    // Endpoint publico (sin [Authorize]) para que la app procese la
    // aceptacion o rechazo de una invitacion.
    //
    // --- ¿Por que sigue siendo PUBLICO, sin verificar quien llama? ---
    // Es, deliberadamente, el MISMO modelo de confianza que ya tenia esta
    // ruta antes del refactor: quien conoce el link/token de la invitacion
    // (recibido de forma privada por Email) esta habilitado a decidir,
    // igual que un link de confirmacion tradicional (ej: "confirmar tu
    // email", "aceptar esta invitacion a un calendario"). Esto permite que
    // alguien SIN cuenta todavia pueda rechazar (o aceptar) una herencia
    // apenas recibe la invitacion, sin verse obligado a registrarse antes
    // solo para poder decidir. El endpoint EQUIVALENTE para un Usuario YA
    // autenticado, que ademas verifica ownership por Token JWT, es
    // "PATCH api/asignaciones/{id}/estado" (AsignacionesController). Que
    // este endpoint sea publico es exactamente por lo que
    // TokenInvitacion tiene que ser no adivinable (ver el comentario en la
    // entidad): es la UNICA proteccion posible en este punto de entrada.
    [HttpPost("{token}/procesar")]
    public async Task<IActionResult> ProcesarInvitacion(string token, [FromBody] ProcesarInvitacionRequest request)
    {
        try
        {
            EstadoBeneficiario nuevoEstado;

            if (request.Accion.Equals("rechazar", StringComparison.OrdinalIgnoreCase))
            {
                nuevoEstado = EstadoBeneficiario.Rechazado;
            }
            else if (request.Accion.Equals("aceptar", StringComparison.OrdinalIgnoreCase))
            {
                nuevoEstado = EstadoBeneficiario.Aceptado;
            }
            else
            {
                return BadRequest(new { mensaje = "Accion invalida. Utilice 'aceptar' o 'rechazar'." });
            }

            // A diferencia de la version anterior (que en "rechazar" borraba
            // directamente la fila de Beneficiario), aca se preserva la fila
            // de AsignacionHerencia y solo se actualiza su Estado: perder el
            // registro de "que activo, en que porcentaje, le fue ofrecido a
            // quien" no tiene sentido ni siquiera cuando la respuesta es un
            // rechazo (el otorgante sigue necesitando saber que ese reparto
            // fue rechazado, para poder reasignarlo a otra persona).
            await _asignacionHerenciaService.CambiarEstadoPorTokenAsync(token, nuevoEstado);

            var mensaje = nuevoEstado == EstadoBeneficiario.Rechazado
                ? "Invitacion rechazada con exito."
                : "Invitacion aceptada con exito.";

            return Ok(new { mensaje });
        }
        catch (RecursoNoEncontradoException)
        {
            return NotFound(new { mensaje = "La invitacion no existe." });
        }
        catch (ReglaNegocioException ex)
        {
            // Por ejemplo, si esta invitacion ya habia sido aceptada o
            // rechazada antes (la regla critica de
            // AsignacionHerenciaService.CambiarEstadoInternoAsync: "el
            // estado ya fue procesado y no puede modificarse").
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al procesar la invitacion con token {Token}.", token);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    // GET api/invitaciones/mis-herencias
    //
    // Endpoint protegido que devuelve el listado de herencias asignadas al
    // usuario logueado. Con el modelo de doble rol, esto ya NO necesita
    // matchear por Email contra una tabla aparte: el propio Usuario
    // autenticado ES el beneficiario (AsignacionHerencia.UsuarioId), asi que
    // alcanza con pedirle al servicio "mis asignaciones recibidas" con el Id
    // que ya viene, verificado, en el Token JWT.
    [Authorize]
    [HttpGet("mis-herencias")]
    public async Task<ActionResult<IEnumerable<MiHerenciaDTO>>> ObtenerMisHerencias()
    {
        try
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (claim is null || !int.TryParse(claim.Value, out var usuarioAutenticadoId))
            {
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            var herencias = await _asignacionHerenciaService.ObtenerAsignacionesPorUsuarioBeneficiarioAsync(usuarioAutenticadoId);

            var dtos = new List<MiHerenciaDTO>();

            // N+1 deliberado: se resuelven Nombre del otorgante y
            // Nombre/Tipo del activo por cada fila, en vez de extender el
            // DTO general "AsignacionHerenciaDTO" con estos datos (que solo
            // este endpoint puntual necesita). Dado el volumen de datos de
            // este proyecto (decenas de filas, no miles), el costo extra de
            // estas consultas es insignificante frente a la simplicidad de
            // no acoplar el DTO general a un caso de uso puntual.
            foreach (var herencia in herencias)
            {
                var titular = await _usuarioService.ObtenerUsuarioPorIdAsync(herencia.UsuarioOtorganteId);
                var activo = await _activoDigitalService.ObtenerActivoDigitalPorIdAsync(herencia.ActivoDigitalId);

                dtos.Add(new MiHerenciaDTO
                {
                    AsignacionId = herencia.Id,
                    TitularNombre = titular.Nombre,
                    ActivoNombre = activo.Nombre,
                    ActivoTipo = (int)activo.Tipo,
                    Porcentaje = herencia.PorcentajeAsignado,
                    CondicionLiberacion = herencia.CondicionLiberacion,
                    Estado = herencia.Estado.ToString()
                });
            }

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener mis herencias.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al obtener las herencias." });
        }
    }
}
