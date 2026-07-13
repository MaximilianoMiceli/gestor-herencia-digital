using System.Security.Claims;
using Herencia.Business.Dtos;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Herencia.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Herencia.Api.Controllers;

/// <summary>Expone el recurso Usuario por HTTP: alta, consulta, actualizacion y seguridad de cuenta.</summary>
/// <remarks>
/// Los datos de un Usuario (Nombre, Email) son informacion personal, por lo que se protege
/// con [Authorize] a nivel de clase ("secure by default"), marcando explicitamente con
/// [AllowAnonymous] el unico endpoint que necesita quedar publico (Crear).
/// </remarks>
[ApiController]
[Authorize]
[Route("api/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;

    // Se inyecta tambien IActivoDigitalService porque este controller expone la ruta anidada
    // "GET api/usuarios/{id}/activos" (los activos digitales de un usuario puntual).
    private readonly IActivoDigitalService _activoDigitalService;

    private readonly ILogger<UsuariosController> _logger;

    public UsuariosController(
        IUsuarioService usuarioService,
        IActivoDigitalService activoDigitalService,
        ILogger<UsuariosController> logger)
    {
        _usuarioService = usuarioService;
        _activoDigitalService = activoDigitalService;
        _logger = logger;
    }

    private int? ObtenerUsuarioIdAutenticado()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return (claim is not null && int.TryParse(claim.Value, out var usuarioId)) ? usuarioId : null;
    }

    /// <summary>Lista todos los usuarios del sistema (solo Administrador).</summary>
    [Authorize(Roles = nameof(RolUsuario.Administrador))]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioDTO>>> ObtenerTodos()
    {
        try
        {
            var usuarios = await _usuarioService.ObtenerTodosLosUsuariosAsync();

            return Ok(usuarios);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al obtener el listado de usuarios.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    /// <summary>Obtiene un usuario por Id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsuarioDTO>> ObtenerPorId(int id)
    {
        try
        {
            // Ownership: un usuario solo puede ver su propio perfil (el sistema no tiene, para
            // este endpoint, otra regla de acceso). El Id de la URL se compara contra el del token.
            var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

            if (usuarioAutenticadoId is null)
            {
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            if (id != usuarioAutenticadoId)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para acceder a este usuario." });
            }

            var usuario = await _usuarioService.ObtenerUsuarioPorIdAsync(id);

            return Ok(usuario);
        }
        catch (RecursoNoEncontradoException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al obtener el usuario con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    /// <summary>Lista los activos digitales de un usuario puntual.</summary>
    [HttpGet("{id:int}/activos")]
    public async Task<ActionResult<IEnumerable<ActivoDigitalDTO>>> ObtenerActivosDelUsuario(int id)
    {
        try
        {
            // Misma verificacion de ownership que ObtenerPorId: un usuario solo puede listar
            // los suyos. El endpoint hermano "GET /api/activos" resuelve el mismo caso de uso
            // sin Id en la URL; esta ruta anidada se mantiene por compatibilidad.
            var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

            if (usuarioAutenticadoId is null)
            {
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            if (id != usuarioAutenticadoId)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para acceder a los activos de este usuario." });
            }

            var activos = await _activoDigitalService.ObtenerActivosPorUsuarioAsync(id);

            return Ok(activos);
        }
        catch (RecursoNoEncontradoException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al obtener los activos del usuario con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    /// <summary>Crea una cuenta de usuario nueva.</summary>
    /// <remarks>
    /// [AllowAnonymous]: unico endpoint publico del controller (no tendria sentido exigir
    /// estar autenticado para crear una cuenta). El flujo de auto-registro "oficial" para un
    /// visitante anonimo es POST /api/auth/registro (AuthController), que reutiliza este
    /// mismo servicio; este endpoint se mantiene por compatibilidad con el alta administrativa
    /// ya implementada antes de existir el modulo de autenticacion.
    /// </remarks>
    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<UsuarioDTO>> Crear(UsuarioCreacionDTO usuarioCreacionDTO)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var usuarioCreado = await _usuarioService.CrearUsuarioAsync(usuarioCreacionDTO);

            return CreatedAtAction(
                nameof(ObtenerPorId),
                new { id = usuarioCreado.Id },
                usuarioCreado);
        }
        catch (ReglaNegocioException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al crear un usuario.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    /// <summary>Actualiza los datos de perfil de un usuario existente.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UsuarioDTO>> Actualizar(int id, UsuarioActualizacionDTO usuarioActualizacionDTO)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Ownership: un usuario solo puede editar su propio perfil.
            var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

            if (usuarioAutenticadoId is null)
            {
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            if (id != usuarioAutenticadoId)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para modificar este usuario." });
            }

            var usuarioActualizado = await _usuarioService.ActualizarUsuarioAsync(id, usuarioActualizacionDTO);

            return Ok(usuarioActualizado);
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
            _logger.LogError(ex, "Error inesperado al actualizar el usuario con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    /// <summary>Cambia la contraseña de un usuario, requiriendo la contraseña actual.</summary>
    [HttpPut("{id:int}/password")]
    public async Task<IActionResult> CambiarPassword(int id, CambiarPasswordDTO cambiarPasswordDTO)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Ownership: un usuario solo puede cambiar su propia contraseña.
            var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

            if (usuarioAutenticadoId is null)
            {
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            if (id != usuarioAutenticadoId)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para cambiar la contraseña de este usuario." });
            }

            await _usuarioService.CambiarPasswordAsync(id, cambiarPasswordDTO);

            return NoContent();
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
            _logger.LogError(ex, "Error inesperado al cambiar la contraseña del usuario con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    /// <summary>Activa o desactiva el doble factor de autenticacion de un usuario.</summary>
    [HttpPut("{id:int}/doble-factor")]
    public async Task<ActionResult<UsuarioDTO>> ActualizarDobleFactor(int id, ActualizarDobleFactorDTO actualizarDobleFactorDTO)
    {
        try
        {
            var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

            if (usuarioAutenticadoId is null)
            {
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            if (id != usuarioAutenticadoId)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para modificar la configuracion de este usuario." });
            }

            var usuarioActualizado = await _usuarioService.ActualizarDobleFactorAsync(id, actualizarDobleFactorDTO.Habilitado);

            return Ok(usuarioActualizado);
        }
        catch (RecursoNoEncontradoException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al actualizar la configuracion de doble factor del usuario con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    /// <summary>Elimina la cuenta de un usuario.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            // Ownership: un usuario solo puede eliminar su propia cuenta.
            var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

            if (usuarioAutenticadoId is null)
            {
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            if (id != usuarioAutenticadoId)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para eliminar este usuario." });
            }

            await _usuarioService.EliminarUsuarioAsync(id);

            return NoContent();
        }
        catch (RecursoNoEncontradoException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al eliminar el usuario con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }
}
