using Herencia.Business.Dtos;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Herencia.Data.Models;
using Microsoft.AspNetCore.Mvc;

namespace Herencia.Api.Controllers;

/// <summary>
/// Expone los endpoints publicos de autenticacion: registro, login (con 2FA opcional) y
/// recuperacion de contraseña. No lleva [Authorize]: seria contradictorio exigir un JWT
/// para poder obtener el primer JWT.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;
    private readonly ISeguridadService _seguridadService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUsuarioService usuarioService,
        ISeguridadService seguridadService,
        ITokenService tokenService,
        ILogger<AuthController> logger)
    {
        _usuarioService = usuarioService;
        _seguridadService = seguridadService;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>Crea una cuenta de usuario nueva (auto-registro).</summary>
    [HttpPost("registro")]
    public async Task<ActionResult<UsuarioDTO>> Registro(RegistroDTO registroDTO)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var usuarioCreado = await _usuarioService.CrearUsuarioAsync(registroDTO);

            return CreatedAtAction(
                actionName: nameof(UsuariosController.ObtenerPorId),
                controllerName: "Usuarios",
                routeValues: new { id = usuarioCreado.Id },
                value: usuarioCreado);
        }
        catch (ReglaNegocioException ex)
        {
            // Warning (no Error): dato invalido del cliente, no falla del servidor.
            _logger.LogWarning(ex, "Registro de usuario rechazado: {Mensaje}", ex.Message);
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al registrar un nuevo usuario.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    /// <summary>
    /// Verifica credenciales y, si son validas, emite un Token JWT (o dispara el segundo
    /// factor si esta habilitado).
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<TokenRespuestaDTO>> Login(LoginDTO loginDTO)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            UsuarioAutenticacionDTO usuarioAutenticacion;

            try
            {
                usuarioAutenticacion = await _usuarioService.ObtenerUsuarioParaAutenticacionAsync(loginDTO.Email);
            }
            catch (RecursoNoEncontradoException)
            {
                // Mismo mensaje que "contraseña incorrecta": evita "user enumeration".
                return Unauthorized(new { mensaje = "Credenciales invalidas." });
            }

            var credencialesValidas = _seguridadService.VerificarPasswordHash(
                loginDTO.Password,
                usuarioAutenticacion.PasswordHash,
                usuarioAutenticacion.PasswordSalt);

            if (!credencialesValidas)
            {
                return Unauthorized(new { mensaje = "Credenciales invalidas." });
            }

            // 2FA: si esta habilitado se envia un codigo y se corta el flujo con RequiereDobleFactor=true;
            // el cliente completa el login con POST /api/auth/verificar-doble-factor.
            if (usuarioAutenticacion.DobleFactorHabilitado)
            {
                await _usuarioService.GenerarYEnviarCodigoDobleFactorAsync(usuarioAutenticacion.Id);

                return Ok(new TokenRespuestaDTO
                {
                    RequiereDobleFactor = true,
                    UsuarioId = usuarioAutenticacion.Id
                });
            }

            // Objeto Usuario transitorio (nunca persistido) solo para pasarle datos a ITokenService.CrearToken.
            var usuarioParaToken = new Usuario
            {
                Id = usuarioAutenticacion.Id,
                Nombre = usuarioAutenticacion.Nombre,
                Email = usuarioAutenticacion.Email,
                Rol = usuarioAutenticacion.Rol
            };

            var token = _tokenService.CrearToken(usuarioParaToken);

            return Ok(new TokenRespuestaDTO { Token = token });
        }
        catch (AutenticacionException ex)
        {
            // Fallo de configuracion del servidor (ej: falta la clave de firma), no del cliente.
            _logger.LogError(ex, "Error al generar el token de autenticacion.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado durante el login.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    /// <summary>Completa el login validando el codigo de doble factor enviado por email.</summary>
    [HttpPost("verificar-doble-factor")]
    public async Task<ActionResult<TokenRespuestaDTO>> VerificarDobleFactor(VerificarDobleFactorDTO verificarDobleFactorDTO)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var usuarioDTO = await _usuarioService.VerificarCodigoDobleFactorAsync(
                verificarDobleFactorDTO.UsuarioId, verificarDobleFactorDTO.Codigo);

            var usuarioParaToken = new Usuario
            {
                Id = usuarioDTO.Id,
                Nombre = usuarioDTO.Nombre,
                Email = usuarioDTO.Email,
                Rol = usuarioDTO.Rol
            };

            var token = _tokenService.CrearToken(usuarioParaToken);

            return Ok(new TokenRespuestaDTO { Token = token });
        }
        catch (RecursoNoEncontradoException)
        {
            return Unauthorized(new { mensaje = "El codigo de verificacion es invalido o ya expiro." });
        }
        catch (ReglaNegocioException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (AutenticacionException ex)
        {
            _logger.LogError(ex, "Error al generar el token de autenticacion tras verificar el doble factor.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al verificar el doble factor.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    /// <summary>Primer paso del flujo de "olvide mi contraseña": solicita el envio de un enlace de reseteo.</summary>
    // Siempre devuelve el mismo mensaje de exito exista o no el email (anti "user enumeration").
    [HttpPost("olvide-password")]
    public async Task<IActionResult> OlvidePassword(SolicitarResetPasswordDTO solicitarResetPasswordDTO)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var token = await _usuarioService.SolicitarResetPasswordAsync(solicitarResetPasswordDTO.Email);

            // Solo si existe una cuenta con ese email (token no nulo) se "envia" el correo simulado.
            if (token is not null)
            {
                var link = $"http://localhost:8081/resetear-password?token={token}";
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine();
                Console.WriteLine("======================================================================");
                Console.WriteLine($"[EMAIL SIMULADO] Reseteo de contraseña solicitado para: {solicitarResetPasswordDTO.Email}");
                Console.WriteLine("Enlace para elegir una nueva contraseña (valido por 1 hora):");
                Console.WriteLine($"👉 {link}");
                Console.WriteLine("======================================================================");
                Console.WriteLine();
                Console.ResetColor();
            }

            return Ok(new { mensaje = "Si el email ingresado corresponde a una cuenta registrada, vas a recibir un enlace para resetear tu contraseña." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al solicitar el reseteo de contraseña.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    /// <summary>Segundo y ultimo paso del flujo de "olvide mi contraseña": aplica la nueva contraseña usando el token recibido por email.</summary>
    [HttpPost("resetear-password")]
    public async Task<IActionResult> ResetearPassword(ResetearPasswordDTO resetearPasswordDTO)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _usuarioService.ResetearPasswordAsync(resetearPasswordDTO);

            return Ok(new { mensaje = "Contraseña actualizada con exito. Ya podes iniciar sesion con tu nueva contraseña." });
        }
        catch (ReglaNegocioException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al resetear la contraseña.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }
}
