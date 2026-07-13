namespace Herencia.Data.Models;

/// <summary>Campos de auditoria comunes a todas las entidades del dominio (quien creo/modifico un registro y cuando).</summary>
public abstract class EntidadBaseAuditable
{
    public DateTime FechaCreacion { get; set; }

    // String simple (no claim de JWT) para no acoplar la capa Data a la capa de seguridad.
    public string UsuarioCreacion { get; set; } = string.Empty;

    public DateTime? FechaModificacion { get; set; }

    public string? UsuarioModificacion { get; set; }
}
