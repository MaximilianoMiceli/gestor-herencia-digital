namespace Herencia.Data.Models;

/// <summary>
/// Resultado de la revision de UN CertificadoDefuncion. Mismo criterio que
/// EstadoBeneficiario: enum arrancando en 1, persistido como INTEGER.
/// </summary>
public enum EstadoCertificadoDefuncion
{
    /// <summary>Recien subido, sin revisar todavia.</summary>
    Pendiente = 1,

    /// <summary>Un Administrador confirmo el documento: dispara la liberacion de bienes.</summary>
    Aprobado = 2,

    /// <summary>Un Administrador determino que el documento no es valido.</summary>
    Rechazado = 3,

    // El titular volvio a confirmar actividad mientras el certificado seguia
    // Pendiente: se cancela solo, sin intervencion de un Administrador, pero la
    // fila se conserva como registro de que hubo una falsa alarma.

    /// <summary>El titular hizo check-in antes de que el certificado fuera revisado.</summary>
    CanceladoPorActividad = 4
}
