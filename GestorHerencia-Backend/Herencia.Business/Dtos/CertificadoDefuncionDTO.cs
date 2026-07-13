using Herencia.Data.Models;

namespace Herencia.Business.Dtos;

// CertificadoDefuncionDTO es el "contrato" de salida de un certificado de
// defuncion subido: lo que ve un Administrador en su cola de revision, o el
// heredero que lo subio al consultar su estado.
public class CertificadoDefuncionDTO
{
    public int Id { get; set; }

    public int UsuarioTitularId { get; set; }

    public string UsuarioTitularNombre { get; set; } = string.Empty;

    public int SubidoPorUsuarioId { get; set; }

    public string SubidoPorNombre { get; set; } = string.Empty;

    public string NombreArchivoOriginal { get; set; } = string.Empty;

    // Se completa a partir de la FechaCreacion heredada de la entidad.
    public DateTime FechaSubida { get; set; }

    public EstadoCertificadoDefuncion Estado { get; set; }

    public int? RevisadoPorUsuarioId { get; set; }

    public DateTime? FechaRevision { get; set; }

    public string? MotivoRechazo { get; set; }
}
