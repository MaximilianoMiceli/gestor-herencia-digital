using System.ComponentModel.DataAnnotations;

namespace Herencia.Business.Dtos;

// CambiarPasswordDTO es el contrato de entrada de PUT /api/usuarios/{id}/password:
// el flujo de cambio de contraseña para un Usuario YA AUTENTICADO que conoce
// su contraseña actual (a diferencia del flujo de "olvide mi contraseña",
// ver SolicitarResetPasswordDTO/ResetearPasswordDTO, pensado para cuando NO
// se la recuerda).
//
// --- ¿Por que pedir "PasswordActual" si el usuario ya tiene un Token JWT valido? ---
// El Token JWT demuestra "quien sos" en este momento, pero no necesariamente
// que segues siendo el dueño legitimo de la sesion: si alguien deja una
// pestaña abierta con sesion iniciada (o roba un token todavia no
// expirado), exigir la contraseña ACTUAL antes de poder cambiarla a una
// nueva evita que ese atacante se "adueñe" permanentemente de la cuenta
// simplemente cambiando la contraseña por una que solo el conoce.
public class CambiarPasswordDTO
{
    [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
    public string PasswordActual { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    public string PasswordNueva { get; set; } = string.Empty;
}
