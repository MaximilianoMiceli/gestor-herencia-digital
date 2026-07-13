namespace Herencia.Data.Models;

/// <summary>
/// Clase base con los campos de auditoria comunes a todas las entidades del
/// dominio (quien creo/modifico un registro y cuando), para no repetirlos en cada
/// modelo.
/// </summary>
public abstract class EntidadBaseAuditable
{
    /// <summary>Fecha y hora (UTC) de creacion del registro. Se completa una unica vez, en el alta.</summary>
    public DateTime FechaCreacion { get; set; }

    // En un sistema real vendria del claim del JWT autenticado; se modela como
    // string simple para no acoplar la capa Data a la capa de seguridad.

    /// <summary>Identificador del usuario que creo el registro.</summary>
    public string UsuarioCreacion { get; set; } = string.Empty;

    /// <summary>Fecha y hora (UTC) de la ultima modificacion. Null si el registro nunca fue modificado.</summary>
    public DateTime? FechaModificacion { get; set; }

    /// <summary>Usuario de la ultima modificacion. Null por el mismo motivo que FechaModificacion.</summary>
    public string? UsuarioModificacion { get; set; }
}
