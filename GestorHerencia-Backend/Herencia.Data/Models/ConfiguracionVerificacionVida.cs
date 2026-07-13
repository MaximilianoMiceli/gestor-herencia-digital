namespace Herencia.Data.Models;

/// <summary>
/// Configuracion de monitoreo de actividad de un titular: cada cuanto debe confirmar que
/// sigue activo, por que canal, y a que contacto escalar si no responde.
/// </summary>
// Relacion 1-1 con PK compartida (UsuarioId es PK y FK): no tiene sentido un Id propio
// porque hay, a lo sumo, una configuracion por usuario.
public class ConfiguracionVerificacionVida : EntidadBaseAuditable
{
    public int UsuarioId { get; set; }

    public Usuario Usuario { get; set; } = null!;

    /// <summary>Si el monitoreo esta activo. El job de escaneo ignora las filas con Activo = false.</summary>
    public bool Activo { get; set; }

    public int FrecuenciaMeses { get; set; }

    public MetodoNotificacion Metodo { get; set; }

    /// <summary>Contacto de confianza al que se escala si el titular no responde. Null hasta elegirlo.</summary>
    public int? ContactoConfianzaId { get; set; }

    public Usuario? ContactoConfianza { get; set; }

    public DateTime UltimoCheckIn { get; set; }

    public EstadoVerificacionVida Estado { get; set; } = EstadoVerificacionVida.Activo;

    public int RecordatoriosEnviados { get; set; }

    public DateTime? FechaUltimoRecordatorio { get; set; }

    public DateTime? FechaProtocoloActivado { get; set; }
}
