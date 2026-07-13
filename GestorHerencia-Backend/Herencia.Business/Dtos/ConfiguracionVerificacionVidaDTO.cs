using Herencia.Data.Models;

namespace Herencia.Business.Dtos;

public class ConfiguracionVerificacionVidaDTO
{
    public int UsuarioId { get; set; }

    public bool Activo { get; set; }

    public int FrecuenciaMeses { get; set; }

    public MetodoNotificacion Metodo { get; set; }

    public int? ContactoConfianzaId { get; set; }

    // Resuelto por el servicio para evitar una consulta aparte del cliente.
    public string? ContactoConfianzaNombre { get; set; }

    public DateTime UltimoCheckIn { get; set; }

    public EstadoVerificacionVida Estado { get; set; }

    public int RecordatoriosEnviados { get; set; }

    public DateTime? FechaUltimoRecordatorio { get; set; }

    public DateTime? FechaProtocoloActivado { get; set; }
}
