using Herencia.Data.Models;

namespace Herencia.Business.Dtos;

// BeneficiarioDTO es el "contrato" de salida de un Beneficiario: la version
// aplanada y controlada de la entidad de Data, sin sus propiedades de
// navegacion (Usuario completo, AsignacionesHerencia), siguiendo el mismo
// criterio que ActivoDigitalDTO.
public class BeneficiarioDTO
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Parentesco { get; set; } = string.Empty;

    // Se expone el Estado (Pendiente/Aceptado/Rechazado) para que quien
    // consuma la Api (ej. el frontend) pueda mostrar el estado actual del
    // flujo de aceptacion sin necesitar una consulta aparte.
    public EstadoBeneficiario Estado { get; set; }

    // Solo se expone el Id del Usuario titular (no el objeto completo), igual
    // que ActivoDigitalDTO.UsuarioId.
    public int UsuarioId { get; set; }
}
