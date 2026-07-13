using System.ComponentModel.DataAnnotations;

namespace Herencia.Business.Dtos;

// A diferencia de Aprobar, Rechazar exige un motivo: quien subio el
// documento necesita saber que corregir.
public class RechazarCertificadoDTO
{
    [Required(ErrorMessage = "El motivo del rechazo es obligatorio.")]
    public string Motivo { get; set; } = string.Empty;
}
