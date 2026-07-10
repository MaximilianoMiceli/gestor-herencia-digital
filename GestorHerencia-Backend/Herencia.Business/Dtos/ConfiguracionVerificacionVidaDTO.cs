using Herencia.Data.Models;

namespace Herencia.Business.Dtos;

// ConfiguracionVerificacionVidaDTO es el "contrato" de salida: lo que
// efectivamente ve el titular (o el propio sistema) al consultar su
// configuracion de monitoreo de actividad.
public class ConfiguracionVerificacionVidaDTO
{
    public int UsuarioId { get; set; }

    public bool Activo { get; set; }

    public int FrecuenciaMeses { get; set; }

    public MetodoNotificacion Metodo { get; set; }

    public int? ContactoConfianzaId { get; set; }

    // Nombre del contacto de confianza, resuelto por el servicio para que
    // el cliente no necesite una consulta aparte a "GET /api/usuarios/{id}"
    // solo para mostrarlo en pantalla.
    public string? ContactoConfianzaNombre { get; set; }

    public DateTime UltimoCheckIn { get; set; }

    public EstadoVerificacionVida Estado { get; set; }

    public int RecordatoriosEnviados { get; set; }

    public DateTime? FechaUltimoRecordatorio { get; set; }

    public DateTime? FechaProtocoloActivado { get; set; }
}
