namespace Herencia.Data.Models;

// Representa el activo digital a heredar: puede ser una cuenta bancaria,
// una red social, una billetera de criptomonedas, un correo electronico, etc.
public class ActivoDigital : EntidadBaseAuditable
{
    public int Id { get; set; }

    // Nombre descriptivo del activo (ej: "Cuenta Banco Santander", "Instagram personal").
    public string Nombre { get; set; } = string.Empty;

    // Categoria del activo, usando el enum TipoActivoDigital para evitar valores libres invalidos.
    public TipoActivoDigital Tipo { get; set; }

    // Descripcion/notas adicionales sobre el activo (ej: instrucciones de acceso,
    // plataforma, numero de cuenta parcial, etc).
    public string Descripcion { get; set; } = string.Empty;

    // --- Archivo adjunto opcional (ej: escaneo de un contrato, PDF con
    // instrucciones notariales) ---
    // RutaArchivo: la ruta/clave FISICA donde IAlmacenamientoArchivosService
    // guardo el archivo (ver AlmacenamientoLocalService). Nunca se expone tal
    // cual en un DTO de salida (mismo criterio que CertificadoDefuncion.
    // RutaArchivo): es un detalle de infraestructura del servidor, no algo
    // que el cliente necesite conocer. Nullable porque adjuntar un archivo es
    // siempre OPCIONAL: la inmensa mayoria de los activos (cuentas bancarias,
    // redes sociales) no tienen ningun archivo asociado, solo texto.
    public string? RutaArchivo { get; set; }

    // NombreArchivoOriginal: el nombre con el que el usuario subio el
    // archivo (ej: "contrato_notarial.pdf"), preservado UNICAMENTE como
    // metadato de exhibicion en el frontend. El nombre real en disco es
    // siempre un Guid nuevo (ver AlmacenamientoLocalService), nunca este.
    public string? NombreArchivoOriginal { get; set; }

    // --- Lado "N" de la relacion 1-N Usuario(Otorgante) -> ActivoDigital ---
    // FK hacia el Usuario propietario del activo, actuando en su rol de
    // OTORGANTE (ver el comentario detallado de doble rol en Usuario.cs). Se
    // sigue llamando "UsuarioId"/"Usuario" (no "OtorganteId"/"Otorgante") por
    // consistencia con el resto de la Api: sigue siendo, ante todo, una
    // referencia simple a la entidad Usuario.
    public int UsuarioId { get; set; }

    public Usuario Usuario { get; set; } = null!;

    // --- Lado "N-N" de la relacion ActivoDigital <-> Usuario(Beneficiario) ---
    // Un mismo ActivoDigital puede repartirse entre varios beneficiarios
    // (ej: 50% para un hijo, 50% para otro), de ahi que necesitemos la tabla
    // intermedia AsignacionHerencia en vez de una FK simple. Notar que el
    // "otro lado" de esta relacion N-N ya NO es una entidad "Beneficiario"
    // separada: es el mismo Usuario, actuando esta vez en su rol de
    // BENEFICIARIO (ver AsignacionHerencia.UsuarioId).
    public ICollection<AsignacionHerencia> AsignacionesHerencia { get; set; } = new List<AsignacionHerencia>();
}
