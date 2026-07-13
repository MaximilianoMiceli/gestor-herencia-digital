namespace Herencia.Data.Models;

/// <summary>
/// Tabla intermedia N-N entre ActivoDigital y Usuario: cada fila es un reparto puntual de
/// un activo hacia un beneficiario (porcentaje, condicion, estado de aceptacion).
/// </summary>
public class AsignacionHerencia : EntidadBaseAuditable
{
    public int Id { get; set; }

    public int ActivoDigitalId { get; set; }

    public ActivoDigital ActivoDigital { get; set; } = null!;

    /// <summary>FK hacia el Usuario beneficiario. Null mientras la persona invitada no tenga cuenta.</summary>
    public int? UsuarioId { get; set; }

    public Usuario? Usuario { get; set; }

    /// <summary>Email de invitacion; se completa siempre y sirve de llave de reclamo automatico al registrarse.</summary>
    public string EmailInvitado { get; set; } = string.Empty;

    public decimal PorcentajeAsignado { get; set; }

    public string CondicionLiberacion { get; set; } = string.Empty;

    public EstadoBeneficiario Estado { get; set; } = EstadoBeneficiario.Pendiente;

    /// <summary>Fecha de liberacion de bienes hacia el beneficiario. Null hasta que se apruebe el certificado de defuncion.</summary>
    public DateTime? FechaLiberacion { get; set; }

    /// <summary>Token publico de invitacion, usado en vez del Id interno en los endpoints sin JWT.</summary>
    public string TokenInvitacion { get; set; } = string.Empty;
}
