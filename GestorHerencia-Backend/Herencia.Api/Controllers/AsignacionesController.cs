using System.Security.Claims;
using Herencia.Business.Dtos;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Herencia.Api.Controllers;

// AsignacionesController expone las operaciones sobre UNA AsignacionHerencia
// puntual, identificada por su propio Id (PUT/DELETE). La creacion (en lote,
// transaccional) y el listado por ActivoDigital viven, en cambio, anidados
// bajo ActivosDigitalesController ("POST/GET api/activosdigitales/{id}/asignaciones"),
// porque conceptualmente son operaciones sobre el "detalle" de un "maestro"
// especifico. Este controller separado existe para las acciones que operan
// sobre una fila puntual de ese detalle, ya identificada por su propio Id,
// sin necesitar conocer de antemano a que ActivoDigital pertenece.
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

    // PUT api/asignaciones/{id}
    //
    // Verbo PUT: actualiza Porcentaje/Condicion de UNA asignacion EXISTENTE.
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

            // Verificacion de OWNERSHIP: se resuelve consultando la
            // asignacion (que ya trae el UsuarioId de su ActivoDigital
            // relacionado, ver AsignacionHerenciaDTO), sin necesitar que el
            // cliente indique por separado a que activo/titular pertenece.
            var asignacionExistente = await _asignacionHerenciaService.ObtenerAsignacionPorIdAsync(id);

            if (asignacionExistente.UsuarioId != usuarioAutenticadoId)
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

    // DELETE api/asignaciones/{id}
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

            if (asignacionExistente.UsuarioId != usuarioAutenticadoId)
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
