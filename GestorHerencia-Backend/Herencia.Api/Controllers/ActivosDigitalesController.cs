using System.Security.Claims;
using Herencia.Business.Dtos;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Herencia.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Herencia.Api.Controllers;

/// <summary>
/// Expone el recurso ActivoDigital por HTTP. El listado de activos de un usuario puntual
/// (sin paginar) vive como ruta anidada en <see cref="UsuariosController"/>
/// ("GET api/usuarios/{id}/activos").
/// </summary>
/// <remarks>
/// Un ActivoDigital es informacion personal y sensible (cuentas bancarias, billeteras
/// cripto, redes sociales), por lo que se protege con [Authorize] a nivel de CLASE
/// ("secure by default"): protege todos los actions, incluidos los que se agreguen a
/// futuro, salvo que se marquen explicitamente con [AllowAnonymous].
/// </remarks>
[ApiController]
[Authorize]
[Route("api/activosdigitales")]
public class ActivosDigitalesController : ControllerBase
{
    private readonly IActivoDigitalService _activoDigitalService;

    // Se inyecta ademas IAsignacionHerenciaService para exponer, anidada bajo este mismo
    // recurso, la relacion maestro-detalle ActivoDigital -> AsignacionHerencia (ver
    // ObtenerAsignaciones/CrearAsignaciones mas abajo).
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

    // Extrae el Id del usuario autenticado del Claim ClaimTypes.NameIdentifier que
    // TokenService.CrearToken empaqueto en el JWT. Es un valor no falsificable sin
    // invalidar la firma del token: la base de todas las verificaciones de ownership
    // de este controller.
    private int? ObtenerUsuarioIdAutenticado()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return (claim is not null && int.TryParse(claim.Value, out var usuarioId)) ? usuarioId : null;
    }

    /// <summary>
    /// Lista, paginados y opcionalmente filtrados por tipo/nombre, los activos digitales
    /// del usuario autenticado.
    /// </summary>
    /// <remarks>
    /// Ruta absoluta "~/api/activos" (ignora el prefijo del controller, requerida por la
    /// consigna). No recibe ningun Id de usuario: siempre opera sobre el usuario del
    /// token, para que sea imposible pedir los activos de otro usuario.
    /// </remarks>
    [HttpGet("~/api/activos")]
    public async Task<ActionResult<ResultadoPaginadoDTO<ActivoDigitalDTO>>> ObtenerMisActivosPaginado(
        [FromQuery] int pagina = 1,
        [FromQuery] int limite = 10,
        [FromQuery] TipoActivoDigital? tipo = null,
        [FromQuery] string? nombre = null)
    {
        try
        {
            var usuarioId = ObtenerUsuarioIdAutenticado();

            if (usuarioId is null)
            {
                // Defensivo: no deberia ocurrir con tokens emitidos por esta Api.
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            var resultado = await _activoDigitalService.ObtenerActivosPorUsuarioPaginadoAsync(usuarioId.Value, pagina, limite, tipo, nombre);

            return Ok(resultado);
        }
        catch (RecursoNoEncontradoException ex)
        {
            // Solo ocurriria si el usuario del token fue eliminado despues de emitirse el token.
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al obtener los activos paginados del usuario autenticado.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    /// <summary>Obtiene un activo digital puntual por su Id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ActivoDigitalDTO>> ObtenerPorId(int id)
    {
        try
        {
            var activoDigital = await _activoDigitalService.ObtenerActivoDigitalPorIdAsync(id);

            // Verificacion de ownership (IDOR): [Authorize] solo garantiza un token valido de
            // ALGUN usuario, no que sea el dueño de ESTE activo puntual. Sin este chequeo,
            // cualquier usuario autenticado podria enumerar Ids y leer activos ajenos.
            var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

            if (usuarioAutenticadoId is null)
            {
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            if (activoDigital.UsuarioId != usuarioAutenticadoId)
            {
                // 403: token valido, pero sin permiso sobre este recurso puntual. No se revela
                // informacion adicional sobre el activo ajeno.
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para acceder a este activo digital." });
            }

            return Ok(activoDigital);
        }
        catch (RecursoNoEncontradoException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al obtener el activo digital con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    /// <summary>Crea un nuevo activo digital para el usuario autenticado.</summary>
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

        // Nunca confiar en el UsuarioId del body: se sobreescribe con el del token para que
        // sea imposible crear un activo a nombre de otro usuario (IDOR en creacion).
        activoDigitalCreacionDTO.UsuarioId = usuarioAutenticadoId.Value;

        try
        {
            var activoCreado = await _activoDigitalService.CrearActivoDigitalAsync(activoDigitalCreacionDTO);

            return CreatedAtAction(
                nameof(ObtenerPorId),
                new { id = activoCreado.Id },
                activoCreado);
        }
        catch (RecursoNoEncontradoException ex)
        {
            // El UsuarioId del body no corresponde a ningun Usuario existente.
            return NotFound(new { mensaje = ex.Message });
        }
        catch (ReglaNegocioException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al crear un activo digital.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    /// <summary>Actualiza Nombre, Tipo y Descripcion de un activo digital existente.</summary>
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

            var activoExistente = await _activoDigitalService.ObtenerActivoDigitalPorIdAsync(id);

            if (activoExistente.UsuarioId != usuarioAutenticadoId)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para modificar este activo digital." });
            }

            var activoActualizado = await _activoDigitalService.ActualizarActivoDigitalAsync(id, activoDigitalActualizacionDTO);

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
            _logger.LogError(ex, "Error inesperado al actualizar el activo digital con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    /// <summary>Elimina el activo digital identificado por el Id.</summary>
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

            var activoExistente = await _activoDigitalService.ObtenerActivoDigitalPorIdAsync(id);

            if (activoExistente.UsuarioId != usuarioAutenticadoId)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para eliminar este activo digital." });
            }

            await _activoDigitalService.EliminarActivoDigitalAsync(id);

            return NoContent();
        }
        catch (RecursoNoEncontradoException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al eliminar el activo digital con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    /// <summary>Lista las asignaciones de herencia (beneficiarios) de un activo digital.</summary>
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

    /// <summary>
    /// Crea, en una unica operacion atomica, un lote de asignaciones que reparte el activo
    /// "{id}" entre uno o mas beneficiarios invitados por email.
    /// </summary>
    /// <remarks>
    /// CrearAsignacionesAsync procesa todo el lote en una unica transaccion: si cualquier
    /// elemento resulta invalido, se revierte completo y ninguna asignacion queda persistida.
    /// </remarks>
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

            var activoDigital = await _activoDigitalService.ObtenerActivoDigitalPorIdAsync(id);

            if (activoDigital.UsuarioId != usuarioAutenticadoId)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para repartir este activo digital." });
            }

            var asignacionesCreadas = (await _asignacionHerenciaService.CrearAsignacionesAsync(id, asignacionesCreacionDTO)).ToList();

            // Notificacion simulada por consola (no hay proveedor de email real integrado):
            // si el beneficiario todavia no tiene cuenta se le "envia" un link de invitacion
            // con el TokenInvitacion (no el Id secuencial, para que no sea enumerable); si ya
            // tiene cuenta, se simula una notificacion in-app.
            foreach (var asignacion in asignacionesCreadas)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine();
                Console.WriteLine("======================================================================");

                if (asignacion.UsuarioBeneficiarioId is null)
                {
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

            // No hay un unico Id "principal" al crear varias filas a la vez: el Location
            // apunta al listado de asignaciones del activo.
            return CreatedAtAction(
                nameof(ObtenerAsignaciones),
                new { id },
                asignacionesCreadas);
        }
        catch (RecursoNoEncontradoException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (ReglaNegocioException ex)
        {
            // Porcentaje invalido, suma superior al 100%, o auto-asignacion.
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al crear asignaciones para el activo digital con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    /// <summary>
    /// Adjunta (o reemplaza) el archivo (PDF/JPG/PNG) de un activo digital existente.
    /// </summary>
    /// <remarks>
    /// El archivo anterior queda huerfano en disco en vez de sobreescribirse (el nombre en
    /// disco siempre es nuevo, ver AlmacenamientoLocalService): aceptable para el alcance de
    /// este proyecto.
    /// </remarks>
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

    /// <summary>Descarga el archivo adjunto de un activo digital.</summary>
    /// <remarks>
    /// A diferencia del resto del controller, aca hay dos formas legitimas de acceso: ser el
    /// titular del activo, o ser un heredero cuya asignacion sobre este activo ya esta
    /// Aceptada y liberada (FechaLiberacion != null, confirmado el fallecimiento del
    /// titular). Un heredero que solo acepto la invitacion pero cuyo otorgante sigue con
    /// vida no puede descargarlo.
    /// </remarks>
    [HttpGet("{id:int}/archivo")]
    public async Task<IActionResult> ObtenerArchivo(int id)
    {
        try
        {
            var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

            if (usuarioAutenticadoId is null)
            {
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            var activoDigital = await _activoDigitalService.ObtenerActivoDigitalPorIdAsync(id);
            var esTitular = activoDigital.UsuarioId == usuarioAutenticadoId;
            var tieneAccesoComoHeredero = false;

            if (!esTitular)
            {
                var misHerencias = await _asignacionHerenciaService.ObtenerAsignacionesPorUsuarioBeneficiarioAsync(usuarioAutenticadoId.Value);

                tieneAccesoComoHeredero = misHerencias.Any(a =>
                    a.ActivoDigitalId == id &&
                    a.Estado == EstadoBeneficiario.Aceptado &&
                    a.FechaLiberacion is not null);
            }

            if (!esTitular && !tieneAccesoComoHeredero)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para acceder al archivo de este activo digital." });
            }

            var (rutaArchivo, nombreOriginal) = await _activoDigitalService.ObtenerArchivoAsync(id);

            if (string.IsNullOrEmpty(rutaArchivo) || !System.IO.File.Exists(rutaArchivo))
            {
                return NotFound(new { mensaje = "Este activo no tiene ningun archivo adjunto disponible." });
            }

            var contentType = ObtenerContentType(nombreOriginal);
            var contenido = System.IO.File.OpenRead(rutaArchivo);
            return File(contenido, contentType, nombreOriginal);
        }
        catch (RecursoNoEncontradoException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al obtener el archivo del activo digital con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    // Infiere el Content-Type a partir de la extension del nombre ORIGINAL guardado (nunca
    // la del archivo en disco, que es un Guid sin extension), ya que SubirArchivoAsync ya
    // restringio de antemano los tipos posibles a PDF/JPG/PNG.
    private static string ObtenerContentType(string nombreArchivoOriginal)
    {
        var extension = Path.GetExtension(nombreArchivoOriginal).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
    }
}
