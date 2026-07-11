using Herencia.Data.Models;

namespace Herencia.Business.Dtos;

// UsuarioAutenticacionDTO es un DTO estrictamente INTERNO del flujo de LOGIN:
// a diferencia de UsuarioDTO (el DTO de salida "publico", que un controller
// puede devolver tranquilamente en una respuesta HTTP), este SI incluye
// PasswordHash y PasswordSalt.
//
// ¿Por que esta bien que este DTO cargue esos dos campos, si en UsuarioDTO
// dijimos explicitamente que jamas debian salir? Porque UsuarioAutenticacionDTO
// nunca llega a serializarse como respuesta JSON hacia un cliente: viaja
// UNICAMENTE desde UsuarioService (que lo arma a partir de la entidad Usuario)
// hacia AuthController (que lo usa, en memoria, para llamar a
// ISeguridadService.VerificarPasswordHash), y se descarta ahi mismo. Es,
// conceptualmente, un "canal privado" entre Business y el endpoint de Login,
// no parte del contrato publico de la Api.
public class UsuarioAutenticacionDTO
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public byte[] PasswordHash { get; set; } = [];

    public byte[] PasswordSalt { get; set; } = [];

    // Se incluye el Rol aca (y no solo en UsuarioDTO) porque AuthController lo
    // necesita para armar el Claim de rol del JWT en el momento del login
    // (ver TokenService.CrearToken): el token debe reflejar el rol REAL del
    // usuario en la base de datos en ese instante, no un valor default.
    public RolUsuario Rol { get; set; }

    // AuthController.Login lo necesita para decidir si, tras validar la
    // contraseña, corresponde cortar el flujo y pedir el segundo factor
    // (ver GenerarYEnviarCodigoDobleFactorAsync) en vez de emitir el JWT
    // directamente.
    public bool DobleFactorHabilitado { get; set; }
}
