using System.Security.Claims;
using Herencia.Business.Dtos;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Herencia.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Herencia.Api.Controllers;

/// <summary>Expone la subida y revision de certificados de defuncion.</summary>
/// <remarks>
/// Subir solo requiere [Authorize] simple: la propia regla de negocio
/// (CertificadoDefuncionService.SubirCertificadoAsync exige que quien sube sea un heredero
/// ya aceptado del titular indicado) ya impide subir a nombre de alguien sin relacion.
/// Aprobar/Rechazar, en cambio, requieren el rol Administrador: un heredero no puede
/// autoaprobar su propio certificado.
/// </remarks>
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

    /// <summary>Sube el certificado de defuncion (PDF/JPG/PNG) de un titular.</summary>
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
            // El heredero que sube el documento es siempre el usuario autenticado, nunca un
            // valor del body (mismo criterio que ActivosDigitalesController.Crear).
            await using var contenido = archivo.OpenReadStream();

            var certificado = await _certificadoDefuncionService.SubirCertificadoAsync(
                usuarioTitularId,
                usuarioAutenticadoId.Value,
                contenido,
                archivo.FileName,
                archivo.ContentType,
                archivo.Length);

            // Sin header Location: no existe un "GET certificado por Id" para un heredero comun.
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

    /// <summary>Lista los certificados de defuncion pendientes de revision (solo Administrador).</summary>
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

    /// <summary>Aprueba un certificado de defuncion pendiente (solo Administrador).</summary>
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

    /// <summary>Descarga el binario del certificado (PDF/JPG/PNG) para que un Administrador pueda revisarlo.</summary>
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

    // Determina el Content-Type a partir de la extension del nombre original guardado (nunca
    // la del archivo en disco, un Guid sin extension confiable): alcanza porque
    // SubirCertificadoAsync ya restringio los tipos posibles a PDF/JPG/PNG.
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

    /// <summary>Rechaza un certificado de defuncion pendiente, con motivo (solo Administrador).</summary>
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
