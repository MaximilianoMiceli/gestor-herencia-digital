namespace Herencia.Data.Models;

/// <summary>
/// Configuracion de monitoreo de actividad de un titular: cada cuanto debe confirmar
/// que sigue activo, por que canal se le avisa, y a que contacto de confianza
/// escalar si no responde.
/// </summary>
// Relacion 1-1 con PK compartida (UsuarioId es a la vez PK y FK), en vez de
// agregar estos campos a Usuario: tiene su propio ciclo de vida (se crea/edita
// solo al configurar el monitoreo) y evita ensanchar Usuario con datos que la
// mayoria de sus lecturas no necesitan. Un Id propio no tendria sentido porque la
// relacion es, por definicion, 1 a 1 (cero o una configuracion por usuario).
public class ConfiguracionVerificacionVida : EntidadBaseAuditable
{
    public int UsuarioId { get; set; }

    public Usuario Usuario { get; set; } = null!;

    /// <summary>Si el monitoreo esta activo. El job de escaneo ignora las filas con Activo = false.</summary>
    public bool Activo { get; set; }

    /// <summary>Frecuencia de confirmacion de actividad en meses (3, 6 o 12; validado en VerificacionVidaService).</summary>
    public int FrecuenciaMeses { get; set; }

    public MetodoNotificacion Metodo { get; set; }

    // Nullable: el titular puede guardar la configuracion con el monitoreo
    // desactivado, sin haber elegido contacto todavia. Debe ser un beneficiario ya
    // Aceptado de algun activo de este titular (regla validada tambien en el frontend).

    /// <summary>Contacto de confianza al que se escala si el titular no responde.</summary>
    public int? ContactoConfianzaId { get; set; }

    public Usuario? ContactoConfianza { get; set; }

    /// <summary>Ultima confirmacion de actividad; punto de partida para calcular el vencimiento (+ FrecuenciaMeses).</summary>
    public DateTime UltimoCheckIn { get; set; }

    public EstadoVerificacionVida Estado { get; set; } = EstadoVerificacionVida.Activo;

    /// <summary>Recordatorios enviados desde el ultimo check-in (se resetea en cada check-in).</summary>
    public int RecordatoriosEnviados { get; set; }

    /// <summary>Fecha del ultimo recordatorio; inicio del plazo final antes de activar el protocolo.</summary>
    public DateTime? FechaUltimoRecordatorio { get; set; }

    /// <summary>Fecha en la que se activo el protocolo (Estado paso a EsperandoCertificado).</summary>
    public DateTime? FechaProtocoloActivado { get; set; }
}
