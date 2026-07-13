using System.ComponentModel.DataAnnotations;

namespace Herencia.Business.Dtos;

// Separado de UsuarioCreacionDTO: no incluye "Password" (cambiar la
// contraseña tiene su propio endpoint dedicado, ver CambiarPasswordDTO), para
// que un PUT de edicion basica no pueda terminar reseteando credenciales.
public class UsuarioActualizacionDTO
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio.")]
    public string Email { get; set; } = string.Empty;

    // Mismas reglas de validacion que en el alta (ver UsuarioService.ActualizarUsuarioAsync).
    [Required(ErrorMessage = "El DNI es obligatorio.")]
    public string Dni { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
    public DateTime FechaNacimiento { get; set; }
}
