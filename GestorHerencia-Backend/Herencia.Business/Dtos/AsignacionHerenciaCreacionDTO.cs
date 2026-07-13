using System.ComponentModel.DataAnnotations;

namespace Herencia.Business.Dtos;

// No incluye "ActivoDigitalId" (viaja en la URL). Se invita por "EmailBeneficiario"
// porque la persona invitada puede todavia no tener cuenta creada.
public class AsignacionHerenciaCreacionDTO
{
    [Required(ErrorMessage = "El email del beneficiario es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email del beneficiario no tiene un formato valido.")]
    public string EmailBeneficiario { get; set; } = string.Empty;

    // Rango y tope de suma del 100% por activo se validan en el servicio.
    [Range(0.01, 100, ErrorMessage = "El porcentaje asignado debe ser mayor a 0 y menor o igual a 100.")]
    public decimal PorcentajeAsignado { get; set; }

    public string CondicionLiberacion { get; set; } = string.Empty;
}
