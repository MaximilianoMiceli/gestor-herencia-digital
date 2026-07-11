using System.ComponentModel.DataAnnotations;

namespace Herencia.Business.Dtos;

// VerificarDobleFactorDTO: body de POST /api/auth/verificar-doble-factor.
// Es un endpoint PUBLICO (todavia no hay JWT en este punto del login), asi
// que el cliente debe indicar explicitamente de QUIEN es el codigo que esta
// confirmando (UsuarioId, devuelto por TokenRespuestaDTO en el login inicial).
public class VerificarDobleFactorDTO
{
    [Required(ErrorMessage = "El identificador de usuario es obligatorio.")]
    public int UsuarioId { get; set; }

    [Required(ErrorMessage = "El codigo de verificacion es obligatorio.")]
    public string Codigo { get; set; } = string.Empty;
}
