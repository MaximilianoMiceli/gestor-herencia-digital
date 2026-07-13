using System.ComponentModel.DataAnnotations;

namespace Herencia.Business.Dtos;

// Segundo paso de "olvide mi contraseña". No hace falta el Email ni la
// contraseña anterior: el Token, unico y de vida corta, ya demuestra que
// quien lo tiene accedio a esa bandeja de entrada.
public class ResetearPasswordDTO
{
    [Required(ErrorMessage = "El token de reseteo es obligatorio.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    public string PasswordNueva { get; set; } = string.Empty;
}
