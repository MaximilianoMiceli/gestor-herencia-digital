namespace Herencia.Data.Models;

/// <summary>
/// Documento subido por un heredero para acreditar el fallecimiento de un titular. Un
/// Administrador debe revisarlo y aprobarlo antes de liberar bienes.
/// </summary>
public class CertificadoDefuncion : EntidadBaseAuditable
{
    public int Id { get; set; }

    /// <summary>El titular fallecido, en su rol de OTORGANTE.</summary>
    public int UsuarioTitularId { get; set; }

    public Usuario UsuarioTitular { get; set; } = null!;

    /// <summary>El heredero que subio el documento (debe tener una AsignacionHerencia Aceptada sobre un activo del titular).</summary>
    public int SubidoPorUsuarioId { get; set; }

    public Usuario SubidoPor { get; set; } = null!;

    // Ruta generada por IAlmacenamientoArchivosService, nunca el nombre original: evita colisiones y path traversal.
    public string RutaArchivo { get; set; } = string.Empty;

    public string NombreArchivoOriginal { get; set; } = string.Empty;

    public EstadoCertificadoDefuncion Estado { get; set; } = EstadoCertificadoDefuncion.Pendiente;

    /// <summary>Administrador que aprobo o rechazo el certificado. Null mientras siga Pendiente.</summary>
    public int? RevisadoPorUsuarioId { get; set; }

    public Usuario? RevisadoPor { get; set; }

    public DateTime? FechaRevision { get; set; }

    /// <summary>Motivo de rechazo o de cancelacion automatica por actividad. Null para los demas estados.</summary>
    public string? MotivoRechazo { get; set; }
}
