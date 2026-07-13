using System.Security.Claims;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Herencia.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Herencia.Api.Controllers;

/// <summary>DTO con los datos de una invitacion para la tarjeta de invitacion del cliente movil.</summary>
public class InvitacionDTO
{
    // Se expone el TOKEN (no el Id interno): la app lo reutiliza en POST /api/invitaciones/{token}/procesar.
    public string Token { get; set; } = string.Empty;
    public string EmisorNombre { get; set; } = string.Empty;
    public string BeneficiarioNombre { get; set; } = string.Empty;
    public string BeneficiarioEmail { get; set; } = string.Empty;
}

/// <summary>DTO que representa una herencia recibida para el listado de "Mis herencias".</summary>
public class MiHerenciaDTO
{
    public int AsignacionId { get; set; }

    // Id del ActivoDigital: necesario para pedir su archivo adjunto (GET
    // /api/activosdigitales/{id}/archivo) una vez que Disponible es true.
    public int ActivoDigitalId { get; set; }

    // Id del Usuario titular/otorgante: necesario para subir el certificado de defuncion de
    // este titular puntual (POST /api/certificados-defuncion exige "usuarioTitularId").
    public int TitularId { get; set; }
    public string TitularNombre { get; set; } = string.Empty;
    public string ActivoNombre { get; set; } = string.Empty;
    public int ActivoTipo { get; set; }
    public decimal Porcentaje { get; set; }
    public string CondicionLiberacion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;

    // true si el otorgante ya fallecio (certificado aprobado) y el bien quedo liberado: recien
    // ahi el heredero puede considerarlo "disponible", mas alla de haber aceptado la invitacion.
    public bool Disponible { get; set; }

    // Descripcion puede contener datos sensibles reales del activo (clave privada de una
    // wallet, CBU, credenciales): exponerlos a un heredero que acepto pero cuyo otorgante
    // sigue con vida seria una fuga de informacion. Por eso quedan null hasta Disponible == true.
    public string? Descripcion { get; set; }
    public string? NombreArchivoOriginal { get; set; }
}

/// <summary>Cuerpo de la solicitud para aceptar o rechazar una invitacion.</summary>
public class ProcesarInvitacionRequest
{
    public string Accion { get; set; } = string.Empty; // "aceptar" o "rechazar"
}

/// <summary>
/// Expone la consulta y confirmacion de invitaciones de herencia por token publico.
/// </summary>
// El "Id" aca es el TokenInvitacion (no adivinable), nunca el Id entero autoincremental:
// los endpoints publicos no verifican ownership por JWT, solo que el token sea correcto.
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

    /// <summary>Obtiene los datos de la tarjeta de invitacion a partir del token, sin requerir sesion iniciada.</summary>
    [HttpGet("{token}")]
    public async Task<ActionResult<InvitacionDTO>> ObtenerInvitacion(string token)
    {
        try
        {
            var asignacion = await _asignacionHerenciaService.ObtenerAsignacionPorTokenAsync(token);

            var emisor = await _usuarioService.ObtenerUsuarioPorIdAsync(asignacion.UsuarioOtorganteId);

            // El beneficiario solo tiene Nombre si ya reclamo la invitacion con una cuenta
            // propia; si todavia no se registro, solo se conoce su Email.
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

    /// <summary>Acepta o rechaza una invitacion identificada por su token.</summary>
    // Publico deliberadamente (como un link de confirmacion por email). El equivalente
    // autenticado, con ownership por JWT, es PATCH api/asignaciones/{id}/estado.
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

            // Se preserva la fila y solo se actualiza su Estado (incluso en un rechazo).
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
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al procesar la invitacion con token {Token}.", token);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    /// <summary>Lista las herencias asignadas al usuario autenticado, con los datos resueltos de titular y activo.</summary>
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

            // N+1 deliberado: volumen bajo, se prefiere no acoplar el DTO general a este caso de uso.
            foreach (var herencia in herencias)
            {
                var titular = await _usuarioService.ObtenerUsuarioPorIdAsync(herencia.UsuarioOtorganteId);
                var activo = await _activoDigitalService.ObtenerActivoDigitalPorIdAsync(herencia.ActivoDigitalId);
                var disponible = herencia.FechaLiberacion is not null;

                dtos.Add(new MiHerenciaDTO
                {
                    AsignacionId = herencia.Id,
                    ActivoDigitalId = herencia.ActivoDigitalId,
                    TitularId = herencia.UsuarioOtorganteId,
                    TitularNombre = titular.Nombre,
                    ActivoNombre = activo.Nombre,
                    ActivoTipo = (int)activo.Tipo,
                    Porcentaje = herencia.PorcentajeAsignado,
                    CondicionLiberacion = herencia.CondicionLiberacion,
                    Estado = herencia.Estado.ToString(),
                    Disponible = disponible,
                    Descripcion = disponible ? activo.Descripcion : null,
                    NombreArchivoOriginal = disponible ? activo.NombreArchivoOriginal : null
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
