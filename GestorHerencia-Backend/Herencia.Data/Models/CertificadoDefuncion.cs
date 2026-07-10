namespace Herencia.Data.Models;

// CertificadoDefuncion representa UN documento subido por un heredero para
// acreditar el fallecimiento de un titular. Puede llegar por dos vias
// (ver CertificadoDefuncionService.SubirCertificadoAsync):
//  a) Proactiva: cualquier heredero ACEPTADO lo sube en cualquier momento,
//     sin depender de que el monitoreo de actividad haya vencido.
//  b) Por escalamiento: el job de VerificacionVidaService, tras agotar
//     recordatorios + plazo final sin respuesta del titular, notifica a
//     los herederos para que lo suban.
// En ambos casos, un Administrador debe revisarlo y aprobarlo antes de que
// se liberen los bienes: la sola subida del archivo NUNCA libera nada por
// si misma.
public class CertificadoDefuncion : EntidadBaseAuditable
{
    public int Id { get; set; }

    // El titular fallecido (dueño de los activos a liberar). FK hacia
    // Usuario, en su rol de OTORGANTE.
    public int UsuarioTitularId { get; set; }

    public Usuario UsuarioTitular { get; set; } = null!;

    // El heredero que subio el documento. Debe tener, al momento de la
    // subida, al menos una AsignacionHerencia en estado Aceptado sobre un
    // ActivoDigital de UsuarioTitularId (ver CertificadoDefuncionService).
    public int SubidoPorUsuarioId { get; set; }

    public Usuario SubidoPor { get; set; } = null!;

    // Ruta/clave con la que IAlmacenamientoArchivosService guardo el
    // archivo fisico. Nunca es el nombre original del archivo (ver
    // AlmacenamientoLocalService): evita colisiones y problemas de path
    // traversal si alguien sube un nombre de archivo manipulado.
    public string RutaArchivo { get; set; } = string.Empty;

    // Nombre original del archivo tal como lo subio el heredero, guardado
    // SOLO como metadato para mostrarlo en el panel de revision del
    // Administrador (ej: "acta_defuncion_juan_perez.pdf").
    public string NombreArchivoOriginal { get; set; } = string.Empty;

    // Resultado de la revision (ver EstadoCertificadoDefuncion).
    public EstadoCertificadoDefuncion Estado { get; set; } = EstadoCertificadoDefuncion.Pendiente;

    // Administrador que aprobo o rechazo este certificado. Nullable porque
    // mientras el Estado siga en Pendiente todavia no lo reviso nadie.
    public int? RevisadoPorUsuarioId { get; set; }

    public Usuario? RevisadoPor { get; set; }

    public DateTime? FechaRevision { get; set; }

    // Motivo del rechazo (obligatorio del lado de negocio cuando Estado
    // termina en Rechazado; nullable aca porque no aplica a los otros
    // estados). Tambien se reutiliza para dejar constancia del motivo
    // cuando el Estado pasa a CanceladoPorActividad automaticamente.
    public string? MotivoRechazo { get; set; }
}
