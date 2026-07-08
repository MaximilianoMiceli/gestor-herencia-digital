using System.ComponentModel.DataAnnotations;

namespace Herencia.Business.Dtos;

// BeneficiarioActualizacionDTO es el "contrato" de entrada para actualizar un
// Beneficiario existente. No incluye UsuarioId: el titular de un beneficiario
// no se reasigna por edicion, mismo criterio que ActivoDigitalActualizacionDTO.
public class BeneficiarioActualizacionDTO
{
    [Required(ErrorMessage = "El nombre del beneficiario es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email del beneficiario es obligatorio.")]
    public string Email { get; set; } = string.Empty;

    public string Parentesco { get; set; } = string.Empty;
}
