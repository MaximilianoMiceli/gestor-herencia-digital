using System.ComponentModel.DataAnnotations;

namespace Herencia.Business.Dtos;

// Endpoint publico (todavia no hay JWT), por eso el cliente debe indicar
// explicitamente UsuarioId (devuelto por TokenRespuestaDTO en el login inicial).
public class VerificarDobleFactorDTO
{
    [Required(ErrorMessage = "El identificador de usuario es obligatorio.")]
    public int UsuarioId { get; set; }

    [Required(ErrorMessage = "El codigo de verificacion es obligatorio.")]
    public string Codigo { get; set; } = string.Empty;
}
