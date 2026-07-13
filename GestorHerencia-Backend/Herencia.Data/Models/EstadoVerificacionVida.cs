namespace Herencia.Data.Models;

/// <summary>
/// Estado de la maquina de estados del monitoreo de actividad de un titular
/// (ConfiguracionVerificacionVida.Estado). Mismo criterio que EstadoBeneficiario:
/// enum arrancando en 1, persistido como INTEGER.
/// </summary>
public enum EstadoVerificacionVida
{
    /// <summary>El titular respondio a tiempo, o su plazo actual todavia no vencio.</summary>
    Activo = 1,

    /// <summary>El plazo vencio y ya se envio al menos un recordatorio, pero no se agotaron todos.</summary>
    RecordatorioEnviado = 2,

    /// <summary>Se agotaron los recordatorios y el plazo final: se pidio a los herederos el certificado.</summary>
    EsperandoCertificado = 3,

    /// <summary>Un heredero subio un certificado, pendiente de revision por un Administrador.</summary>
    CertificadoEnRevision = 4,

    /// <summary>Un Administrador aprobo el certificado: el fallecimiento queda confirmado.</summary>
    FallecimientoConfirmado = 5,

    /// <summary>Se liberaron los bienes hacia todos los herederos aceptados.</summary>
    HerenciaLiberada = 6
}
