using System.ComponentModel.DataAnnotations;

namespace Herencia.Business.Dtos;

// ResetearPasswordDTO es el contrato de entrada de POST /api/auth/resetear-password:
// el segundo y ultimo paso del flujo de "olvide mi contraseña". El cliente
// llega hasta aca con el "Token" que recibio en el link simulado por Email
// (ver UsuarioService.SolicitarResetPasswordAsync) y la nueva contraseña que
// eligio. No hace falta el Email ni la contraseña anterior: el Token, al
// ser un valor unico generado por el servidor y de vida corta, ya demuestra
// por si solo que quien lo tiene efectivamente accedio a la bandeja de
// entrada de esa cuenta.
public class ResetearPasswordDTO
{
    [Required(ErrorMessage = "El token de reseteo es obligatorio.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    public string PasswordNueva { get; set; } = string.Empty;
}
