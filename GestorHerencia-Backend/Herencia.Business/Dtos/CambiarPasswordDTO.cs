using System.ComponentModel.DataAnnotations;

namespace Herencia.Business.Dtos;

// Flujo para un usuario ya autenticado que conoce su contraseña actual (a
// diferencia de "olvide mi contraseña", ver SolicitarResetPasswordDTO). Exigir
// PasswordActual evita que quien robe un token todavia no expirado se adueñe
// de la cuenta cambiando la contraseña.
public class CambiarPasswordDTO
{
    [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
    public string PasswordActual { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    public string PasswordNueva { get; set; } = string.Empty;
}
