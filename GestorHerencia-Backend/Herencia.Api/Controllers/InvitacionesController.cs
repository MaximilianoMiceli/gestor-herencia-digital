using Herencia.Data;
using Herencia.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Herencia.Api.Controllers;

/// <summary>
/// DTO para retornar los datos de la invitación en un formato limpio para el cliente móvil.
/// </summary>
public class InvitacionDTO
{
    public int Id { get; set; }
    public string EmisorNombre { get; set; } = string.Empty;
    public string BeneficiarioNombre { get; set; } = string.Empty;
    public string BeneficiarioEmail { get; set; } = string.Empty;
    public string Parentesco { get; set; } = string.Empty;
}

/// <summary>
/// DTO que representa una herencia recibida para el listado de "Mis herencias" (Frame 24).
/// </summary>
public class MiHerenciaDTO
{
    public int AsignacionId { get; set; }
    public string TitularNombre { get; set; } = string.Empty;
    public string Parentesco { get; set; } = string.Empty;
    public string ActivoNombre { get; set; } = string.Empty;
    public int ActivoTipo { get; set; }
    public decimal Porcentaje { get; set; }
    public string CondicionLiberacion { get; set; } = string.Empty;
    public bool Disponible { get; set; }
}

/// <summary>
/// Modelo de datos recibido para procesar la invitación.
/// </summary>
public class ProcesarInvitacionRequest
{
    public string Accion { get; set; } = string.Empty; // "aceptar" o "rechazar"
}

/// <summary>
/// Controlador público de invitaciones. 
/// Encargado de posibilitar la consulta y confirmación de invitaciones de herencia.
/// Funciona de manera autónoma contra la base de datos sin alterar el esquema actual de tablas.
/// </summary>
[ApiController]
[Route("api/invitaciones")]
public class InvitacionesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<InvitacionesController> _logger;

    public InvitacionesController(AppDbContext context, ILogger<InvitacionesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET api/invitaciones/{id}
    //
    // Endpoint público para que la app cargue los datos de la tarjeta de invitación (Frame 21)
    // sin requerir que el usuario esté logueado aún.
    [HttpGet("{id:int}")]
    public async Task<ActionResult<InvitacionDTO>> ObtenerInvitacion(int id)
    {
        try
        {
            var beneficiario = await _context.Beneficiarios
                .Include(b => b.Usuario)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (beneficiario == null)
            {
                return NotFound(new { mensaje = "La invitación no existe o ha sido revocada." });
            }

            var dto = new InvitacionDTO
            {
                Id = beneficiario.Id,
                EmisorNombre = beneficiario.Usuario.Nombre,
                BeneficiarioNombre = beneficiario.Nombre,
                BeneficiarioEmail = beneficiario.Email,
                Parentesco = beneficiario.Parentesco
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al obtener la invitación {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrió un error interno al procesar la solicitud." });
        }
    }

    // POST api/invitaciones/{id}/procesar
    //
    // Endpoint público para que la app procese la aceptación o rechazo de una invitación.
    [HttpPost("{id:int}/procesar")]
    public async Task<IActionResult> ProcesarInvitacion(int id, [FromBody] ProcesarInvitacionRequest request)
    {
        try
        {
            var beneficiario = await _context.Beneficiarios
                .Include(b => b.Usuario)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (beneficiario == null)
            {
                return NotFound(new { mensaje = "La invitación no existe." });
            }

            if (request.Accion.Equals("rechazar", StringComparison.OrdinalIgnoreCase))
            {
                // Si rechaza la invitación, eliminamos el registro del Beneficiario para romper el vínculo.
                _context.Beneficiarios.Remove(beneficiario);
                await _context.SaveChangesAsync();
                return Ok(new { mensaje = "Invitación rechazada con éxito y removida del sistema." });
            }
            else if (request.Accion.Equals("aceptar", StringComparison.OrdinalIgnoreCase))
            {
                // Aceptar la invitación no requiere cambios en BD ya que el vínculo es por Email.
                // Retornamos éxito para confirmar que la app puede proceder al flujo de autenticación/registro.
                return Ok(new { mensaje = "Invitación aceptada con éxito." });
            }

            return BadRequest(new { mensaje = "Acción inválida. Utilice 'aceptar' o 'rechazar'." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al procesar la invitación {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrió un error interno al procesar la solicitud." });
        }
    }

    // GET api/invitaciones/mis-herencias
    //
    // Endpoint protegido que devuelve el listado de herencias asignadas al usuario logueado.
    // Busca coincidencias de su Email (desde claims) con la tabla Beneficiarios.
    [Authorize]
    [HttpGet("mis-herencias")]
    public async Task<ActionResult<IEnumerable<MiHerenciaDTO>>> ObtenerMisHerencias()
    {
        try
        {
            var emailClaim = User.FindFirst(ClaimTypes.Email);
            if (emailClaim == null)
            {
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (idClaim != null && int.TryParse(idClaim.Value, out var uId))
                {
                    var u = await _context.Usuarios.FindAsync(uId);
                    if (u != null)
                    {
                        emailClaim = new Claim(ClaimTypes.Email, u.Email);
                    }
                }
            }

            if (emailClaim == null || string.IsNullOrEmpty(emailClaim.Value))
            {
                return BadRequest(new { mensaje = "No se pudo determinar el correo electrónico del usuario autenticado." });
            }

            var email = emailClaim.Value;

            var beneficiarios = await _context.Beneficiarios
                .Include(b => b.Usuario)
                .Include(b => b.AsignacionesHerencia)
                    .ThenInclude(ah => ah.ActivoDigital)
                .Where(b => b.Email.ToLower() == email.ToLower())
                .ToListAsync();

            var dtos = new List<MiHerenciaDTO>();

            foreach (var b in beneficiarios)
            {
                if (b.AsignacionesHerencia.Count == 0)
                {
                    dtos.Add(new MiHerenciaDTO
                    {
                        AsignacionId = b.Id * -1, // ID negativo para invitaciones sin activos asignados
                        TitularNombre = b.Usuario.Nombre,
                        Parentesco = b.Parentesco,
                        ActivoNombre = "Ninguno",
                        ActivoTipo = 0,
                        Porcentaje = 0,
                        CondicionLiberacion = "Por definir",
                        Disponible = false
                    });
                }
                else
                {
                    foreach (var a in b.AsignacionesHerencia)
                    {
                        dtos.Add(new MiHerenciaDTO
                        {
                            AsignacionId = a.Id,
                            TitularNombre = b.Usuario.Nombre,
                            Parentesco = b.Parentesco,
                            ActivoNombre = a.ActivoDigital.Nombre,
                            ActivoTipo = (int)a.ActivoDigital.Tipo,
                            Porcentaje = a.PorcentajeAsignado,
                            CondicionLiberacion = a.CondicionLiberacion,
                            Disponible = false
                        });
                    }
                }
            }

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener mis herencias.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrió un error interno al obtener las herencias." });
        }
    }
}
