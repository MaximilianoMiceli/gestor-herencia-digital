namespace Herencia.Data.Models;

/// <summary>Resultado de la revision de un CertificadoDefuncion.</summary>
public enum EstadoCertificadoDefuncion
{
    Pendiente = 1,

    Aprobado = 2,

    Rechazado = 3,

    // Se cancela solo (sin intervencion de un Administrador) si el titular vuelve a
    // confirmar actividad mientras el certificado seguia Pendiente.
    CanceladoPorActividad = 4
}
