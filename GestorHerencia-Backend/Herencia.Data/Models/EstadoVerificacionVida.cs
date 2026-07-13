namespace Herencia.Data.Models;

/// <summary>Estado de la maquina de estados del monitoreo de actividad de un titular (ConfiguracionVerificacionVida.Estado).</summary>
public enum EstadoVerificacionVida
{
    Activo = 1,

    RecordatorioEnviado = 2,

    EsperandoCertificado = 3,

    CertificadoEnRevision = 4,

    FallecimientoConfirmado = 5,

    HerenciaLiberada = 6
}
