namespace Herencia.Data.Models;

// EstadoVerificacionVida representa en que punto de la "maquina de estados"
// del monitoreo de actividad se encuentra el titular (ConfiguracionVerificacionVida.Estado).
// Mismo criterio que EstadoBeneficiario: enum (no bool ni string libre) para
// que el compilador impida un estado inexistente, y arranca en 1 (nunca en
// el 0 por defecto de C#) para poder detectar una fila mal migrada/insertada
// a mano que quedo en el entero crudo "0".
public enum EstadoVerificacionVida
{
    // El titular respondio dentro del plazo (o todavia no vencio su plazo
    // actual). Estado inicial al activar el monitoreo.
    Activo = 1,

    // El plazo (FrecuenciaMeses desde UltimoCheckIn) ya vencio y el sistema
    // ya envio al menos uno de los recordatorios configurados
    // (VerificacionVida:CantidadRecordatorios), pero todavia no se agotaron
    // todos ni el plazo final posterior al ultimo.
    RecordatorioEnviado = 2,

    // Se agotaron los recordatorios Y el plazo final posterior al ultimo
    // (VerificacionVida:DiasPlazoFinalTrasUltimoRecordatorio) sin que el
    // titular haya vuelto a confirmar actividad: el "protocolo" esta
    // activo, y se les solicito a los herederos aceptados que suban el
    // certificado de defuncion.
    EsperandoCertificado = 3,

    // Al menos un heredero ya subio un CertificadoDefuncion y esta
    // pendiente de revision por un Administrador.
    CertificadoEnRevision = 4,

    // Un Administrador aprobo el certificado: el fallecimiento del titular
    // queda confirmado formalmente.
    FallecimientoConfirmado = 5,

    // Se ejecuto la liberacion de bienes hacia todos los herederos
    // aceptados del titular (ver CertificadoDefuncionService.AprobarAsync).
    HerenciaLiberada = 6
}
