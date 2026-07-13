namespace Herencia.Data.Models;

/// <summary>Activo digital a heredar: cuenta bancaria, red social, billetera de criptomonedas, correo electronico, etc.</summary>
public class ActivoDigital : EntidadBaseAuditable
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public TipoActivoDigital Tipo { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    /// <summary>
    /// Ruta/clave fisica donde IAlmacenamientoArchivosService guardo el archivo. Nullable
    /// porque la mayoria de los activos no tienen ningun archivo asociado, solo texto.
    /// </summary>
    public string? RutaArchivo { get; set; }

    public string? NombreArchivoOriginal { get; set; }

    // FK hacia el Usuario en su rol de OTORGANTE (ver el doble rol de Usuario en Usuario.cs).
    public int UsuarioId { get; set; }

    public Usuario Usuario { get; set; } = null!;

    // N-N con Usuario(Beneficiario): un activo puede repartirse entre varios beneficiarios,
    // de ahi la tabla intermedia AsignacionHerencia en vez de una FK simple.
    public ICollection<AsignacionHerencia> AsignacionesHerencia { get; set; } = new List<AsignacionHerencia>();
}
