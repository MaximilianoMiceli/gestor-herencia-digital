namespace Herencia.Data.Models;

// EstadoCertificadoDefuncion representa el resultado de la revision de UN
// CertificadoDefuncion puntual subido por un heredero. Mismo criterio que
// EstadoBeneficiario: enum arrancando en 1, persistido como INTEGER.
public enum EstadoCertificadoDefuncion
{
    // Recien subido, todavia sin revisar por ningun Administrador.
    Pendiente = 1,

    // Un Administrador confirmo que el documento es valido: dispara la
    // liberacion de bienes (ver CertificadoDefuncionService.AprobarAsync).
    Aprobado = 2,

    // Un Administrador determino que el documento NO es valido (ej: no
    // corresponde al titular, esta incompleto, es ilegible).
    Rechazado = 3,

    // El titular volvio a confirmar actividad (check-in) mientras este
    // certificado seguia Pendiente: el pedido se cancela SOLO, sin
    // intervencion de ningun Administrador, pero la fila se conserva (no se
    // borra) para dejar registro de que existio un pedido de liberacion que
    // resulto ser una falsa alarma. Ver VerificacionVidaService.RegistrarCheckInAsync.
    CanceladoPorActividad = 4
}
