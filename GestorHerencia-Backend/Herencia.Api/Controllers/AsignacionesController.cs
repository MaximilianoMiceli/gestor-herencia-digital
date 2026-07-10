using System.Security.Claims;
using Herencia.Business.Dtos;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Herencia.Api.Controllers;

// AsignacionesController expone las operaciones sobre UNA AsignacionHerencia
// puntual, identificada por su propio Id (PUT/DELETE/PATCH). La creacion (en
// lote, transaccional) y el listado por ActivoDigital viven, en cambio,
// anidados bajo ActivosDigitalesController
// ("POST/GET api/activosdigitales/{id}/asignaciones"), porque conceptualmente
// son operaciones sobre el "detalle" de un "maestro" especifico. Este
// controller separado existe para las acciones que operan sobre una fila
// puntual de ese detalle, ya identificada por su propio Id.
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

    // GET api/asignaciones/mis-herencias
    //
    // Devuelve las AsignacionHerencia en las que el usuario AUTENTICADO
    // participa como BENEFICIARIO (Id extraido del Token JWT). Con el modelo
    // de doble rol, cualquier Usuario puede consultar esto sin depender de
    // que un otorgante lo busque "por email": la relacion ya esta resuelta
    // por UsuarioId desde el momento de la creacion (o del registro, si la
    // invitacion se reclamo despues, ver UsuarioService.CrearUsuarioAsync).
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

    // PUT api/asignaciones/{id}
    //
    // Verbo PUT: actualiza Porcentaje/Condicion de UNA asignacion EXISTENTE.
    // Ownership: solo el OTORGANTE (dueño del ActivoDigital repartido) puede
    // modificar estos datos, nunca el beneficiario.
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

            // Verificacion de OWNERSHIP del lado del OTORGANTE: se resuelve
            // consultando la asignacion (que ya trae el UsuarioOtorganteId de
            // su ActivoDigital relacionado, ver AsignacionHerenciaDTO), sin
            // necesitar que el cliente indique por separado a que activo
            // pertenece.
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

    // PATCH api/asignaciones/{id}/estado
    //
    // --- ¿Por que PATCH y no PUT? ---
    // PUT (usado arriba en "Actualizar") representa, por convencion HTTP/
    // REST, un REEMPLAZO COMPLETO del recurso: el cliente envia TODOS los
    // campos editables (Porcentaje, CondicionLiberacion) y el servidor los
    // pisa enteros. PATCH, en cambio, representa una MODIFICACION PARCIAL:
    // el cliente envia unicamente el campo que quiere cambiar (aca, un
    // unico campo: NuevoEstado), dejando todo lo demas intacto. Usar PUT
    // para esta operacion obligaria al cliente a reenviar Porcentaje/
    // CondicionLiberacion solo para poder aceptar o rechazar una herencia
    // (datos que ni siquiera esta tocando), ademas de comunicar una
    // intencion semantica incorrecta: esto NO es "reemplazo el recurso
    // completo", es "transiciono UN campo puntual de su ciclo de vida".
    // Ademas, a diferencia de PUT (siempre idempotente por definicion), la
    // regla de negocio de abajo (estado ya procesado) hace que un segundo
    // PATCH con el MISMO valor no sea un "no-op" silencioso, sino que se
    // rechaza explicitamente con 400: la transicion de estado es un evento
    // de UNA SOLA VEZ, no una simple sobreescritura de datos.
    //
    // --- Ownership: aca el chequeo es AL REVES que en Actualizar/Eliminar ---
    // Quien acepta o rechaza una herencia es el BENEFICIARIO, nunca el
    // otorgante: por eso se compara contra
    // "asignacionExistente.UsuarioBeneficiarioId" (no "UsuarioOtorganteId").
    // Esto es, justamente, lo que el modelo de doble rol permite resolver
    // con propiedad: como el beneficiario ahora ES un Usuario con su propia
    // cuenta y su propio Token JWT, puede autenticarse el mismo para tomar
    // esta decision, sin depender de que el otorgante la tome en su nombre.
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

            // 200 OK (no 204 NoContent): se devuelve el recurso actualizado
            // completo para que el cliente pueda confirmar, sin una consulta
            // GET aparte, el nuevo valor de Estado que efectivamente quedo
            // persistido.
            return Ok(asignacionActualizada);
        }
        catch (RecursoNoEncontradoException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (ReglaNegocioException ex)
        {
            // 400 Bad Request: se dispara, en particular, cuando se viola la
            // regla de transicion de estados (la asignacion ya estaba en
            // "Aceptado" o "Rechazado", se intento fijar "Pendiente" a mano,
            // o la invitacion todavia no fue reclamada por ninguna cuenta).
            // Es una violacion de una REGLA DE NEGOCIO del lado del cliente
            // (esta pidiendo una operacion que no es valida dado el estado
            // ACTUAL del recurso), no un error del servidor: por eso 400 y
            // no 500.
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al cambiar el estado de la asignacion de herencia con Id {Id}.", id);
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
