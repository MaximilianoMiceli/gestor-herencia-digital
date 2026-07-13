using System.ComponentModel.DataAnnotations;

namespace Herencia.Business.Dtos;

// Primer paso de "olvide mi contraseña". La existencia (o no) de una cuenta
// con ese Email nunca se revela en la respuesta (anti "user enumeration",
// mismo criterio que el Login).
public class SolicitarResetPasswordDTO
{
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato valido.")]
    public string Email { get; set; } = string.Empty;
}
