using Herencia.Data.Models;

namespace Herencia.Business.Dtos;

// No incluye "UsuarioId": ese dato sale siempre del Claim del Token JWT, para
// que sea imposible configurar el monitoreo de otro usuario.
public class ConfiguracionVerificacionVidaActualizacionDTO
{
    public bool Activo { get; set; }

    // Solo se aceptan 3, 6 o 12 (validado en VerificacionVidaService.GuardarConfiguracionAsync).
    public int FrecuenciaMeses { get; set; }

    public MetodoNotificacion Metodo { get; set; }

    // Puede quedar sin contacto solo si "Activo" es false; el servicio exige
    // un contacto ACEPTADO para poder activar el monitoreo.
    public int? ContactoConfianzaId { get; set; }
}
