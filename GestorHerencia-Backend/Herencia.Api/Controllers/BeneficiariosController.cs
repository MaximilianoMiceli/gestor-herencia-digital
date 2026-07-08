using System.Security.Claims;
using Herencia.Business.Dtos;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Herencia.Api.Controllers;

// BeneficiariosController expone el recurso "Beneficiario" por HTTP, con
// exactamente el mismo criterio de diseño que ActivosDigitalesController:
// capa delgada, [Authorize] a nivel de clase, y verificacion de OWNERSHIP en
// cada endpoint que lee/modifica un recurso puntual (un usuario solo puede
// gestionar SUS PROPIOS beneficiarios).
//
// Este controller es, ademas, la implementacion concreta de la relacion
// MAESTRO-DETALLE "Usuario (maestro) -> Beneficiario (detalle)": GET
// /api/beneficiarios devuelve, para el usuario autenticado, el detalle
// completo de sus beneficiarios (el listado que colgaria de la pantalla de
// "mi cuenta" en la app).
[ApiController]
[Authorize]
[Route("api/beneficiarios")]
public class BeneficiariosController : ControllerBase
{
    private readonly IBeneficiarioService _beneficiarioService;
    private readonly ILogger<BeneficiariosController> _logger;

    public BeneficiariosController(
        IBeneficiarioService beneficiarioService,
        ILogger<BeneficiariosController> logger)
    {
        _beneficiarioService = beneficiarioService;
        _logger = logger;
    }

    private int? ObtenerUsuarioIdAutenticado()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return (claim is not null && int.TryParse(claim.Value, out var usuarioId)) ? usuarioId : null;
    }

    // GET api/beneficiarios
    //
    // Devuelve los Beneficiarios del usuario AUTENTICADO (Id extraido del
    // Token JWT, nunca de un parametro que el cliente pueda manipular): mismo
    // criterio que "GET /api/activos" en ActivosDigitalesController.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BeneficiarioDTO>>> ObtenerMisBeneficiarios()
    {
        try
        {
            var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

            if (usuarioAutenticadoId is null)
            {
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            var beneficiarios = await _beneficiarioService.ObtenerBeneficiariosPorUsuarioAsync(usuarioAutenticadoId.Value);

            return Ok(beneficiarios);
        }
        catch (RecursoNoEncontradoException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al obtener los beneficiarios del usuario autenticado.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    // GET api/beneficiarios/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BeneficiarioDTO>> ObtenerPorId(int id)
    {
        try
        {
            var beneficiario = await _beneficiarioService.ObtenerBeneficiarioPorIdAsync(id);

            var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

            if (usuarioAutenticadoId is null)
            {
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            // Verificacion de OWNERSHIP (IDOR): un usuario solo puede ver SUS
            // PROPIOS beneficiarios, nunca los de otro titular.
            if (beneficiario.UsuarioId != usuarioAutenticadoId)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para acceder a este beneficiario." });
            }

            return Ok(beneficiario);
        }
        catch (RecursoNoEncontradoException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al obtener el beneficiario con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    // POST api/beneficiarios
    [HttpPost]
    public async Task<ActionResult<BeneficiarioDTO>> Crear(BeneficiarioCreacionDTO beneficiarioCreacionDTO)
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

        // Nunca confiar en el UsuarioId del body: se fuerza al usuario
        // autenticado, igual que en ActivosDigitalesController.Crear.
        beneficiarioCreacionDTO.UsuarioId = usuarioAutenticadoId.Value;

        try
        {
            var beneficiarioCreado = await _beneficiarioService.CrearBeneficiarioAsync(beneficiarioCreacionDTO);

            return CreatedAtAction(
                nameof(ObtenerPorId),
                new { id = beneficiarioCreado.Id },
                beneficiarioCreado);
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
            _logger.LogError(ex, "Error inesperado al crear un beneficiario.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    // PUT api/beneficiarios/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult<BeneficiarioDTO>> Actualizar(int id, BeneficiarioActualizacionDTO beneficiarioActualizacionDTO)
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

            var beneficiarioExistente = await _beneficiarioService.ObtenerBeneficiarioPorIdAsync(id);

            if (beneficiarioExistente.UsuarioId != usuarioAutenticadoId)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para modificar este beneficiario." });
            }

            var beneficiarioActualizado = await _beneficiarioService.ActualizarBeneficiarioAsync(id, beneficiarioActualizacionDTO);

            return Ok(beneficiarioActualizado);
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
            _logger.LogError(ex, "Error inesperado al actualizar el beneficiario con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    // DELETE api/beneficiarios/{id}
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

            var beneficiarioExistente = await _beneficiarioService.ObtenerBeneficiarioPorIdAsync(id);

            if (beneficiarioExistente.UsuarioId != usuarioAutenticadoId)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para eliminar este beneficiario." });
            }

            await _beneficiarioService.EliminarBeneficiarioAsync(id);

            return NoContent();
        }
        catch (RecursoNoEncontradoException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (ReglaNegocioException ex)
        {
            // Caso particular de este endpoint: EliminarBeneficiarioAsync
            // puede lanzar ReglaNegocioException si la base de datos rechaza
            // el borrado (constraint Restrict con AsignacionHerencia). 400
            // Bad Request: la operacion no es valida en el estado actual del
            // recurso, responsabilidad de quien la solicita.
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al eliminar el beneficiario con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }
}
