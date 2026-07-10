using Herencia.Data.Models;

namespace Herencia.Business.Dtos;

// ConfiguracionVerificacionVidaActualizacionDTO es el "contrato" de entrada
// para crear o editar la configuracion de monitoreo del titular AUTENTICADO.
// Deliberadamente NO incluye "UsuarioId": ese dato sale siempre del Claim
// del Token JWT (ver VerificacionVidaController), nunca de un valor que el
// cliente pueda escribir en el body, para que sea fisicamente imposible que
// alguien configure el monitoreo de OTRO usuario.
public class ConfiguracionVerificacionVidaActualizacionDTO
{
    public bool Activo { get; set; }

    // Solo se aceptan 3, 6 o 12 (validado en VerificacionVidaService.GuardarConfiguracionAsync,
    // igual que ya restringe el dropdown del frontend).
    public int FrecuenciaMeses { get; set; }

    public MetodoNotificacion Metodo { get; set; }

    // Nullable: el titular puede guardar la configuracion sin contacto
    // todavia, siempre que "Activo" sea false (el servicio exige un
    // contacto ACEPTADO como condicion para poder activar el monitoreo,
    // misma regla que ya valida verificacion-vida.tsx del lado del cliente).
    public int? ContactoConfianzaId { get; set; }
}
