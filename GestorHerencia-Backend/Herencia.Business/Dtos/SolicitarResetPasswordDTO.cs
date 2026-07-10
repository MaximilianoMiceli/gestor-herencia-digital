using System.ComponentModel.DataAnnotations;

namespace Herencia.Business.Dtos;

// SolicitarResetPasswordDTO es el contrato de entrada de
// POST /api/auth/olvide-password: el primer paso del flujo de "olvide mi
// contraseña", pensado para un visitante SIN sesion iniciada (por eso vive
// en AuthController, no en UsuariosController). Solo pide el Email: la
// existencia (o no) de una cuenta con ese Email nunca se revela en la
// respuesta (ver el comentario de AuthController.OlvidePassword), el mismo
// criterio anti "user enumeration" que ya usa el Login.
public class SolicitarResetPasswordDTO
{
    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email no tiene un formato valido.")]
    public string Email { get; set; } = string.Empty;
}
