using System.ComponentModel.DataAnnotations;

namespace Herencia.Business.Dtos;

// RechazarCertificadoDTO es el "contrato" de entrada para que un
// Administrador rechace un CertificadoDefuncion. A diferencia de Aprobar
// (que no necesita ningun dato adicional en el body: la decision misma ya
// es toda la informacion que hace falta), Rechazar exige un motivo: quien
// subio el documento necesita saber que corregir para volver a intentarlo.
public class RechazarCertificadoDTO
{
    [Required(ErrorMessage = "El motivo del rechazo es obligatorio.")]
    public string Motivo { get; set; } = string.Empty;
}
