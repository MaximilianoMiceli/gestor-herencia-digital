using System.ComponentModel.DataAnnotations;

namespace Herencia.Business.Dtos;

// Para un usuario ya autenticado que conoce su contraseña actual (a diferencia
// de "olvide mi contraseña"); exigirla evita que un token robado tome la cuenta.
public class CambiarPasswordDTO
{
    [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
    public string PasswordActual { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    public string PasswordNueva { get; set; } = string.Empty;
}
