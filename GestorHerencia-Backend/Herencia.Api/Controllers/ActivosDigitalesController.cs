using Herencia.Business.Dtos;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Herencia.Api.Controllers;

// ActivosDigitalesController expone el recurso "ActivoDigital" por HTTP.
// Igual que UsuariosController, es una capa DELGADA: no contiene ninguna
// regla de negocio (esa vive en ActivoDigitalService), solo traduce HTTP <->
// llamadas al servicio. El listado de activos de un usuario puntual vive
// como ruta anidada en UsuariosController ("GET api/usuarios/{id}/activos"),
// por eso no se repite aca.
[ApiController]
[Route("api/activosdigitales")]
public class ActivosDigitalesController : ControllerBase
{
    // Se inyecta UNICAMENTE la interfaz de servicio (IActivoDigitalService),
    // nunca IActivoDigitalRepository ni AppDbContext. Esto mantiene a la base
    // de datos completamente aislada detras de la capa Business: si mañana
    // cambia el motor de base de datos o el ORM, este controller no se entera
    // ni necesita cambiar una sola linea.
    private readonly IActivoDigitalService _activoDigitalService;

    private readonly ILogger<ActivosDigitalesController> _logger;

    public ActivosDigitalesController(
        IActivoDigitalService activoDigitalService,
        ILogger<ActivosDigitalesController> logger)
    {
        _activoDigitalService = activoDigitalService;
        _logger = logger;
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

            // 200 OK: se encontro el activo solicitado.
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
    // Verbo POST: crea un nuevo ActivoDigital. El UsuarioId titular viaja
    // dentro del body (ActivoDigitalCreacionDTO.UsuarioId): el servicio valida,
    // ANTES de persistir, que ese usuario realmente exista (regla de negocio
    // explicita de la rubrica), de modo que el controller ni siquiera necesita
    // saber que esa validacion ocurre.
    [HttpPost]
    public async Task<ActionResult<ActivoDigitalDTO>> Crear(ActivoDigitalCreacionDTO activoDigitalCreacionDTO)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

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
}
