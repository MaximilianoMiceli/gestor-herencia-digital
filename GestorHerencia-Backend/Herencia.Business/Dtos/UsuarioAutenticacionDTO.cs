using Herencia.Data.Models;

namespace Herencia.Business.Dtos;

// A diferencia de UsuarioDTO, este SI incluye PasswordHash/PasswordSalt: nunca
// se serializa hacia un cliente, viaja unicamente de UsuarioService a
// AuthController (para ISeguridadService.VerificarPasswordHash) y se descarta ahi.
public class UsuarioAutenticacionDTO
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public byte[] PasswordHash { get; set; } = [];

    public byte[] PasswordSalt { get; set; } = [];

    // AuthController lo necesita para armar el Claim de rol del JWT (ver TokenService.CrearToken).
    public RolUsuario Rol { get; set; }

    // AuthController.Login lo usa para decidir si cortar el flujo y pedir el
    // segundo factor en vez de emitir el JWT directamente.
    public bool DobleFactorHabilitado { get; set; }
}
