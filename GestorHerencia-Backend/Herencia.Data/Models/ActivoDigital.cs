namespace Herencia.Data.Models;

/// <summary>
/// Activo digital a heredar: cuenta bancaria, red social, billetera de criptomonedas,
/// correo electronico, etc.
/// </summary>
public class ActivoDigital : EntidadBaseAuditable
{
    public int Id { get; set; }

    /// <summary>Nombre descriptivo del activo (ej: "Cuenta Banco Santander").</summary>
    public string Nombre { get; set; } = string.Empty;

    public TipoActivoDigital Tipo { get; set; }

    /// <summary>Notas adicionales (instrucciones de acceso, plataforma, numero de cuenta parcial, etc).</summary>
    public string Descripcion { get; set; } = string.Empty;

    // Adjunto opcional (ej: escaneo de un contrato notarial). Nullable porque la
    // mayoria de los activos (cuentas bancarias, redes sociales) no tienen ningun
    // archivo asociado, solo texto.

    /// <summary>
    /// Ruta/clave fisica donde IAlmacenamientoArchivosService guardo el archivo.
    /// Es un detalle de infraestructura: nunca se expone en un DTO de salida.
    /// </summary>
    public string? RutaArchivo { get; set; }

    /// <summary>Nombre con el que el usuario subio el archivo, preservado solo como metadato de exhibicion.</summary>
    public string? NombreArchivoOriginal { get; set; }

    // FK hacia el Usuario propietario del activo, en su rol de OTORGANTE (ver el
    // doble rol de Usuario en Usuario.cs). Se llama "UsuarioId" y no "OtorganteId"
    // por consistencia con el resto de la Api.
    public int UsuarioId { get; set; }

    public Usuario Usuario { get; set; } = null!;

    // Lado N-N con Usuario(Beneficiario): un mismo activo puede repartirse entre
    // varios beneficiarios, de ahi la tabla intermedia AsignacionHerencia en vez
    // de una FK simple.

    /// <summary>Repartos de este activo entre los distintos beneficiarios.</summary>
    public ICollection<AsignacionHerencia> AsignacionesHerencia { get; set; } = new List<AsignacionHerencia>();
}
