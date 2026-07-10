namespace Herencia.Data.Models;

// ConfiguracionVerificacionVida guarda, para UN titular (Usuario), como
// quiere ser monitoreado: cada cuanto debe confirmar actividad, por que
// canal se le avisa, y a que contacto de confianza escalar si no responde.
//
// --- ¿Por que 1-1 con Usuario (PK compartida) en vez de un campo mas en Usuario? ---
// Se separa de Usuario (en vez de agregarle estos campos directamente) por
// el mismo motivo que AsignacionHerencia es una entidad propia y no columnas
// sueltas: este set de campos tiene su propio ciclo de vida (se crea/edita
// solo cuando el usuario configura el monitoreo, no en el alta de cuenta) y
// mezclar todo en Usuario haria esa entidad cada vez mas ancha con datos que
// la INMENSA mayoria de sus lecturas (login, perfil, activos) ni necesitan.
// Se usa una relacion 1-1 con CLAVE PRIMARIA COMPARTIDA (UsuarioId es a la
// vez PK y FK) en vez de un Id propio + FK separada porque la relacion es,
// por definicion, obligatoriamente 1-a-1: un Usuario tiene CERO o UNA sola
// configuracion, nunca varias, asi que no tiene sentido darle un Id propio
// independiente del Usuario al que pertenece.
public class ConfiguracionVerificacionVida : EntidadBaseAuditable
{
    // Clave primaria de ESTA tabla y, a la vez, clave foranea hacia Usuario
    // (ver el comentario de arriba). Configurado explicitamente asi en
    // AppDbContext.OnModelCreating.
    public int UsuarioId { get; set; }

    public Usuario Usuario { get; set; } = null!;

    // Si el monitoreo esta activo o no. El titular puede desactivarlo en
    // cualquier momento (ej: mientras todavia no eligio un contacto de
    // confianza), en cuyo caso el job de escaneo (VerificacionVidaService.EjecutarEscaneoAsync)
    // directamente ignora esta fila.
    public bool Activo { get; set; }

    // Cada cuantos meses debe el titular confirmar actividad. Solo se
    // aceptan 3, 6 o 12 (validado en VerificacionVidaService, no aca: esta
    // clase es un modelo de persistencia, no el lugar de las reglas de
    // negocio).
    public int FrecuenciaMeses { get; set; }

    // Canal por el que se le envian los recordatorios (ver MetodoNotificacion).
    public MetodoNotificacion Metodo { get; set; }

    // Contacto de confianza: otro Usuario del sistema, que debe ademas ser
    // un beneficiario ya ACEPTADO de algun activo de este titular (misma
    // regla que ya valida el frontend en verificacion-vida.tsx). Es
    // NULLABLE porque el titular puede guardar la configuracion con el
    // monitoreo todavia desactivado, sin haber elegido contacto todavia.
    public int? ContactoConfianzaId { get; set; }

    public Usuario? ContactoConfianza { get; set; }

    // Ultima vez que el titular confirmo actividad (alta de la
    // configuracion, o un check-in posterior). Es el punto de partida para
    // calcular el vencimiento: UltimoCheckIn.AddMonths(FrecuenciaMeses).
    public DateTime UltimoCheckIn { get; set; }

    // En que punto de la maquina de estados del monitoreo esta este
    // titular (ver EstadoVerificacionVida).
    public EstadoVerificacionVida Estado { get; set; } = EstadoVerificacionVida.Activo;

    // Cuantos recordatorios ya se enviaron desde el ULTIMO check-in (se
    // resetea a 0 en cada check-in). Lo usa el job de escaneo para saber si
    // ya toca enviar el proximo recordatorio o si ya se agotaron los
    // configurados (VerificacionVida:CantidadRecordatorios) y corresponde
    // pasar al plazo final.
    public int RecordatoriosEnviados { get; set; }

    // Fecha del ULTIMO recordatorio enviado. Es el punto de partida del
    // plazo final de 30 dias (VerificacionVida:DiasPlazoFinalTrasUltimoRecordatorio):
    // "3 recordatorios luego de la ultima NO respuesta, y a partir de ahi
    // 30 dias" (ver VerificacionVidaService.EjecutarEscaneoAsync).
    public DateTime? FechaUltimoRecordatorio { get; set; }

    // Fecha en la que se activo el protocolo (Estado paso a
    // EsperandoCertificado), es decir, cuando vencio el plazo final sin
    // respuesta y se les pidio el certificado a los herederos.
    public DateTime? FechaProtocoloActivado { get; set; }
}
