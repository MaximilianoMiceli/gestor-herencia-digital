using System.Security.Claims;
using Herencia.Business.Dtos;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Herencia.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Herencia.Api.Controllers;

// CertificadosDefuncionController expone la subida y revision de
// certificados de defuncion.
//
// --- ¿Por que [Authorize] simple (no por Rol) alcanza para Subir? ---
// Cualquier Usuario autenticado puede intentar subir un certificado: no
// hace falta un rol especial, porque la propia regla de negocio
// (CertificadoDefuncionService.SubirCertificadoAsync exige que
// "subidoPorUsuarioId" sea un heredero YA ACEPTADO del titular indicado) ya
// impide que un usuario sin ninguna relacion con ese titular pueda subir
// algo a su nombre. Aprobar/Rechazar, en cambio, SI requieren el rol
// Administrador: un heredero no puede autoaprobar su propio certificado.
[ApiController]
[Authorize]
[Route("api/certificados-defuncion")]
public class CertificadosDefuncionController : ControllerBase
{
    private readonly ICertificadoDefuncionService _certificadoDefuncionService;
    private readonly ILogger<CertificadosDefuncionController> _logger;

    public CertificadosDefuncionController(
        ICertificadoDefuncionService certificadoDefuncionService,
        ILogger<CertificadosDefuncionController> logger)
    {
        _certificadoDefuncionService = certificadoDefuncionService;
        _logger = logger;
    }

    private int? ObtenerUsuarioIdAutenticado()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return (claim is not null && int.TryParse(claim.Value, out var usuarioId)) ? usuarioId : null;
    }

    // POST api/certificados-defuncion
    //
    // Recibe un formulario multipart (no JSON): "usuarioTitularId" identifica
    // de quien es el certificado, y "archivo" es el documento en si (PDF/JPG/PNG).
    [HttpPost]
    public async Task<ActionResult<CertificadoDefuncionDTO>> Subir([FromForm] int usuarioTitularId, [FromForm] IFormFile archivo)
    {
        var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

        if (usuarioAutenticadoId is null)
        {
            return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
        }

        if (archivo is null || archivo.Length == 0)
        {
            return BadRequest(new { mensaje = "Debe adjuntar un archivo." });
        }

        try
        {
            // --- Nunca confiar en un "SubidoPorUsuarioId" del body ---
            // No existe tal campo en el formulario: el heredero que sube el
            // documento es SIEMPRE el usuario autenticado (mismo criterio
            // que ActivosDigitalesController.Crear sobreescribiendo
            // dto.UsuarioId antes de llamar al servicio).
            await using var contenido = archivo.OpenReadStream();

            var certificado = await _certificadoDefuncionService.SubirCertificadoAsync(
                usuarioTitularId,
                usuarioAutenticadoId.Value,
                contenido,
                archivo.FileName,
                archivo.ContentType,
                archivo.Length);

            // 201 Created sin header "Location": no existe, en esta etapa,
            // un endpoint "GET certificado por Id" para un heredero comun
            // (solo el Administrador tiene un listado, y es de TODOS los
            // pendientes, no de uno puntual).
            return StatusCode(StatusCodes.Status201Created, certificado);
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
            _logger.LogError(ex, "Error inesperado al subir un certificado de defuncion.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    // GET api/certificados-defuncion/pendientes
    [Authorize(Roles = nameof(RolUsuario.Administrador))]
    [HttpGet("pendientes")]
    public async Task<ActionResult<IEnumerable<CertificadoDefuncionDTO>>> ObtenerPendientes()
    {
        try
        {
            var pendientes = await _certificadoDefuncionService.ObtenerPendientesAsync();

            return Ok(pendientes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al obtener los certificados de defuncion pendientes.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    // PATCH api/certificados-defuncion/{id}/aprobar
    [Authorize(Roles = nameof(RolUsuario.Administrador))]
    [HttpPatch("{id:int}/aprobar")]
    public async Task<ActionResult<CertificadoDefuncionDTO>> Aprobar(int id)
    {
        try
        {
            var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

            if (usuarioAutenticadoId is null)
            {
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            var certificadoAprobado = await _certificadoDefuncionService.AprobarAsync(id, usuarioAutenticadoId.Value);

            return Ok(certificadoAprobado);
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
            _logger.LogError(ex, "Error inesperado al aprobar el certificado de defuncion con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    // GET api/certificados-defuncion/{id}/archivo
    //
    // Sirve el binario del certificado (PDF/JPG/PNG) para que un
    // Administrador pueda verlo mientras decide si aprobar o rechazar: antes
    // el panel solo mostraba metadatos (titular, quien lo subio, fecha), sin
    // forma de abrir el documento en si.
    [Authorize(Roles = nameof(RolUsuario.Administrador))]
    [HttpGet("{id:int}/archivo")]
    public async Task<IActionResult> ObtenerArchivo(int id)
    {
        try
        {
            var (rutaArchivo, nombreOriginal) = await _certificadoDefuncionService.ObtenerArchivoAsync(id);

            if (!System.IO.File.Exists(rutaArchivo))
            {
                return NotFound(new { mensaje = "El archivo del certificado ya no esta disponible en el servidor." });
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
            _logger.LogError(ex, "Error inesperado al obtener el archivo del certificado de defuncion con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    // Determina el Content-Type a partir de la EXTENSION del nombre original
    // guardado (nunca de la del archivo en disco, que es un Guid sin
    // extension confiable de por si para este chequeo): alcanza porque
    // SubirCertificadoAsync ya restringio de antemano los tipos posibles a
    // PDF/JPG/PNG.
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

    // PATCH api/certificados-defuncion/{id}/rechazar
    [Authorize(Roles = nameof(RolUsuario.Administrador))]
    [HttpPatch("{id:int}/rechazar")]
    public async Task<ActionResult<CertificadoDefuncionDTO>> Rechazar(int id, RechazarCertificadoDTO rechazarDTO)
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

            var certificadoRechazado = await _certificadoDefuncionService.RechazarAsync(id, usuarioAutenticadoId.Value, rechazarDTO.Motivo);

            return Ok(certificadoRechazado);
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
            _logger.LogError(ex, "Error inesperado al rechazar el certificado de defuncion con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }
}
