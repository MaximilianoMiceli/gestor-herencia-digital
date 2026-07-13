namespace Herencia.Data.Models;

/// <summary>
/// Documento subido por un heredero para acreditar el fallecimiento de un titular.
/// Puede llegar de forma proactiva (cualquier heredero aceptado lo sube en cualquier
/// momento) o por escalamiento (tras agotar el monitoreo de VerificacionVidaService).
/// Un Administrador debe revisarlo y aprobarlo antes de liberar bienes: la sola
/// subida nunca libera nada por si misma.
/// </summary>
public class CertificadoDefuncion : EntidadBaseAuditable
{
    public int Id { get; set; }

    /// <summary>El titular fallecido, en su rol de OTORGANTE.</summary>
    public int UsuarioTitularId { get; set; }

    public Usuario UsuarioTitular { get; set; } = null!;

    /// <summary>
    /// El heredero que subio el documento. Debe tener, al momento de la subida, al
    /// menos una AsignacionHerencia Aceptada sobre un activo de UsuarioTitularId.
    /// </summary>
    public int SubidoPorUsuarioId { get; set; }

    public Usuario SubidoPor { get; set; } = null!;

    // Ruta generada por IAlmacenamientoArchivosService, nunca el nombre original:
    // evita colisiones y problemas de path traversal.
    public string RutaArchivo { get; set; } = string.Empty;

    /// <summary>Nombre original del archivo, guardado solo como metadato para el panel de revision.</summary>
    public string NombreArchivoOriginal { get; set; } = string.Empty;

    public EstadoCertificadoDefuncion Estado { get; set; } = EstadoCertificadoDefuncion.Pendiente;

    /// <summary>Administrador que aprobo o rechazo el certificado. Null mientras siga Pendiente.</summary>
    public int? RevisadoPorUsuarioId { get; set; }

    public Usuario? RevisadoPor { get; set; }

    public DateTime? FechaRevision { get; set; }

    /// <summary>
    /// Motivo de rechazo, o del cambio automatico a CanceladoPorActividad. Nullable
    /// porque no aplica a los demas estados.
    /// </summary>
    public string? MotivoRechazo { get; set; }
}
