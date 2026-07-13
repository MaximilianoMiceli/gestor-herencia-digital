using System.ComponentModel.DataAnnotations;

namespace Herencia.Business.Dtos;

// No expone "Id", "PasswordHash/Salt" ni "FechaCreacion": esos campos los
// completa el sistema, no quien llama a la Api (evita overposting).
public class UsuarioCreacionDTO
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio.")]
    public string Email { get; set; } = string.Empty;

    // Texto plano, transitorio: UsuarioService la usa para calcular
    // PasswordHash/Salt y nunca la persiste tal cual.
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = string.Empty;

    // Formato (solo digitos, largo 7-8) se valida en el servicio.
    [Required(ErrorMessage = "El DNI es obligatorio.")]
    public string Dni { get; set; } = string.Empty;

    // El servicio la usa tambien para exigir mayoria de edad (ver UsuarioService.CrearUsuarioAsync).
    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
    public DateTime FechaNacimiento { get; set; }
}
