using System.Security.Claims;
using Herencia.Business.Dtos;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Herencia.Api.Controllers;

/// <summary>
/// Expone las operaciones sobre una AsignacionHerencia puntual por su Id (PUT/PATCH/DELETE).
/// La creacion en lote y el listado por activo viven anidados bajo <see cref="ActivosDigitalesController"/>.
/// </summary>
[ApiController]
[Authorize]
[Route("api/asignaciones")]
public class AsignacionesController : ControllerBase
{
    private readonly IAsignacionHerenciaService _asignacionHerenciaService;
    private readonly ILogger<AsignacionesController> _logger;

    public AsignacionesController(
        IAsignacionHerenciaService asignacionHerenciaService,
        ILogger<AsignacionesController> logger)
    {
        _asignacionHerenciaService = asignacionHerenciaService;
        _logger = logger;
    }

    private int? ObtenerUsuarioIdAutenticado()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return (claim is not null && int.TryParse(claim.Value, out var usuarioId)) ? usuarioId : null;
    }

    /// <summary>Lista las asignaciones de herencia en las que el usuario autenticado participa como beneficiario.</summary>
    [HttpGet("mis-herencias")]
    public async Task<ActionResult<IEnumerable<AsignacionHerenciaDTO>>> ObtenerMisHerenciasRecibidas()
    {
        try
        {
            var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

            if (usuarioAutenticadoId is null)
            {
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            var herencias = await _asignacionHerenciaService.ObtenerAsignacionesPorUsuarioBeneficiarioAsync(usuarioAutenticadoId.Value);

            return Ok(herencias);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al obtener las herencias recibidas del usuario autenticado.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    /// <summary>Actualiza Porcentaje/Condicion de una asignacion existente.</summary>
    /// <remarks>Ownership: solo el otorgante (dueño del activo repartido) puede modificar estos datos, nunca el beneficiario.</remarks>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<AsignacionHerenciaDTO>> Actualizar(int id, AsignacionHerenciaActualizacionDTO asignacionActualizacionDTO)
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

            // Ownership del otorgante resuelto consultando la asignacion, nunca un dato del cliente.
            var asignacionExistente = await _asignacionHerenciaService.ObtenerAsignacionPorIdAsync(id);

            if (asignacionExistente.UsuarioOtorganteId != usuarioAutenticadoId)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para modificar esta asignacion de herencia." });
            }

            var asignacionActualizada = await _asignacionHerenciaService.ActualizarAsignacionAsync(id, asignacionActualizacionDTO);

            return Ok(asignacionActualizada);
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
            _logger.LogError(ex, "Error inesperado al actualizar la asignacion de herencia con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    /// <summary>Acepta o rechaza una asignacion de herencia (transicion de estado).</summary>
    // Ownership invertido: decide el BENEFICIARIO, no el otorgante.
    [HttpPatch("{id:int}/estado")]
    public async Task<ActionResult<AsignacionHerenciaDTO>> CambiarEstado(int id, ActualizarEstadoAsignacionDTO actualizarEstadoDTO)
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

            var asignacionExistente = await _asignacionHerenciaService.ObtenerAsignacionPorIdAsync(id);

            if (asignacionExistente.UsuarioBeneficiarioId != usuarioAutenticadoId)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para modificar el estado de esta asignacion de herencia." });
            }

            var asignacionActualizada = await _asignacionHerenciaService.CambiarEstadoAsync(id, actualizarEstadoDTO.NuevoEstado);

            return Ok(asignacionActualizada);
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
            _logger.LogError(ex, "Error inesperado al cambiar el estado de la asignacion de herencia con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    /// <summary>Elimina la asignacion de herencia identificada por el Id.</summary>
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

            var asignacionExistente = await _asignacionHerenciaService.ObtenerAsignacionPorIdAsync(id);

            if (asignacionExistente.UsuarioOtorganteId != usuarioAutenticadoId)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para eliminar esta asignacion de herencia." });
            }

            await _asignacionHerenciaService.EliminarAsignacionAsync(id);

            return NoContent();
        }
        catch (RecursoNoEncontradoException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al eliminar la asignacion de herencia con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }
}
