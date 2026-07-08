using System.ComponentModel.DataAnnotations;

namespace Herencia.Business.Dtos;

// BeneficiarioCreacionDTO es el "contrato" de entrada para dar de alta un
// nuevo Beneficiario, siguiendo el mismo criterio que ActivoDigitalCreacionDTO:
// no expone Id, FechaCreacion ni AsignacionesHerencia (detalles internos que
// el cliente no deberia poder establecer a mano).
public class BeneficiarioCreacionDTO
{
    [Required(ErrorMessage = "El nombre del beneficiario es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email del beneficiario es obligatorio.")]
    public string Email { get; set; } = string.Empty;

    public string Parentesco { get; set; } = string.Empty;

    // Id del Usuario titular al que se le va a asociar este nuevo
    // beneficiario. Al igual que en ActivoDigitalCreacionDTO.UsuarioId, el
    // controller SOBREESCRIBE este valor con el Id del usuario autenticado
    // (extraido del Token JWT) antes de llamar al servicio: nunca se confia
    // en el UsuarioId que venga en el body (ver BeneficiariosController.Crear).
    public int UsuarioId { get; set; }
}
