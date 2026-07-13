using Herencia.Data.Models;

namespace Herencia.Business.Dtos;

// Fila de la tabla intermedia N-N entre ActivoDigital y Usuario. Este DTO
// combina Ids de ambos roles (otorgante y beneficiario), por eso ninguno se
// llama simplemente "UsuarioId" como en la entidad.
public class AsignacionHerenciaDTO
{
    public int Id { get; set; }

    public int ActivoDigitalId { get; set; }

    // Null si la persona invitada todavia no se registro (ver EmailInvitado).
    public int? UsuarioBeneficiarioId { get; set; }

    // Se expone siempre, aunque UsuarioBeneficiarioId ya este completo.
    public string EmailInvitado { get; set; } = string.Empty;

    public decimal PorcentajeAsignado { get; set; }

    public string CondicionLiberacion { get; set; } = string.Empty;

    public EstadoBeneficiario Estado { get; set; }

    // Titular del ActivoDigital (asignacion.ActivoDigital.UsuarioId), expuesto
    // para que la Api verifique ownership sin una consulta aparte.
    public int UsuarioOtorganteId { get; set; }

    // Identificador publico no adivinable usado para armar el link de invitacion;
    // nunca se usa "Id" para eso.
    public string TokenInvitacion { get; set; } = string.Empty;

    // La fija CertificadoDefuncionService.AprobarAsync; null mientras el
    // titular sigue con vida/activo.
    public DateTime? FechaLiberacion { get; set; }
}
