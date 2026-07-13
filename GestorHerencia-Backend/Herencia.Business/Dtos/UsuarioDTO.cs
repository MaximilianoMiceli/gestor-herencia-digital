using Herencia.Data.Models;

namespace Herencia.Business.Dtos;

// Deliberadamente no incluye "PasswordHash" ni "PasswordSalt": esos campos
// jamas deben salir de la capa Business hacia un cliente externo.
public class UsuarioDTO
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Dni { get; set; } = string.Empty;

    public DateTime FechaNacimiento { get; set; }

    public DateTime FechaCreacion { get; set; }

    public RolUsuario Rol { get; set; }

    // No es informacion sensible (a diferencia de CodigoDobleFactor, que
    // jamas se expone aca); el frontend lo necesita para el toggle de 2FA.
    public bool DobleFactorHabilitado { get; set; }
}
