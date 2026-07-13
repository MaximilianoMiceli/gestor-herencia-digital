using System.Security.Claims;
using Herencia.Business.Dtos;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Herencia.Api.Controllers;

/// <summary>
/// Expone el monitoreo de actividad del titular autenticado: consultar/guardar su
/// configuracion y confirmar actividad (check-in).
/// </summary>
/// <remarks>
/// Todas las operaciones actuan siempre sobre el propio usuario del token, nunca sobre un Id
/// recibido en la ruta o el body: no existe ningun escenario legitimo donde alguien deba
/// poder tocar la configuracion de otro usuario desde este controller.
/// </remarks>
[ApiController]
[Authorize]
[Route("api/verificacion-vida")]
public class VerificacionVidaController : ControllerBase
{
    private readonly IVerificacionVidaService _verificacionVidaService;
    private readonly ILogger<VerificacionVidaController> _logger;

    public VerificacionVidaController(
        IVerificacionVidaService verificacionVidaService,
        ILogger<VerificacionVidaController> logger)
    {
        _verificacionVidaService = verificacionVidaService;
        _logger = logger;
    }

    private int? ObtenerUsuarioIdAutenticado()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return (claim is not null && int.TryParse(claim.Value, out var usuarioId)) ? usuarioId : null;
    }

    /// <summary>Obtiene la configuracion de verificacion de vida del usuario autenticado.</summary>
    [HttpGet("configuracion")]
    public async Task<ActionResult<ConfiguracionVerificacionVidaDTO>> ObtenerConfiguracion()
    {
        try
        {
            var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

            if (usuarioAutenticadoId is null)
            {
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            var configuracion = await _verificacionVidaService.ObtenerConfiguracionAsync(usuarioAutenticadoId.Value);

            return Ok(configuracion);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al obtener la configuracion de verificacion de vida.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    /// <summary>Guarda la configuracion de verificacion de vida del usuario autenticado.</summary>
    [HttpPut("configuracion")]
    public async Task<ActionResult<ConfiguracionVerificacionVidaDTO>> GuardarConfiguracion(
        ConfiguracionVerificacionVidaActualizacionDTO configuracionDTO)
    {
        try
        {
            var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

            if (usuarioAutenticadoId is null)
            {
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            var configuracionGuardada = await _verificacionVidaService.GuardarConfiguracionAsync(usuarioAutenticadoId.Value, configuracionDTO);

            return Ok(configuracionGuardada);
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
            _logger.LogError(ex, "Error inesperado al guardar la configuracion de verificacion de vida.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    /// <summary>Registra un check-in de actividad del usuario autenticado.</summary>
    [HttpPost("check-in")]
    public async Task<ActionResult<ConfiguracionVerificacionVidaDTO>> RegistrarCheckIn()
    {
        try
        {
            var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

            if (usuarioAutenticadoId is null)
            {
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            var configuracionActualizada = await _verificacionVidaService.RegistrarCheckInAsync(usuarioAutenticadoId.Value);

            return Ok(configuracionActualizada);
        }
        catch (RecursoNoEncontradoException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al registrar el check-in de verificacion de vida.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }
}
