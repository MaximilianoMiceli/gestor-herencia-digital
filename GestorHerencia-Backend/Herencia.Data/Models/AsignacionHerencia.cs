namespace Herencia.Data.Models;

// Tabla intermedia (entidad de asociacion) que resuelve la relacion N-N entre
// ActivoDigital y Usuario. Cada fila representa UN reparto puntual: "este
// ActivoDigital le corresponde, en tal porcentaje y bajo tal condicion, a
// este Usuario (actuando como Beneficiario)".
//
// --- ¿Como funciona la tabla intermedia en este escenario de doble rol? ---
// A diferencia de una relacion N-N "de catalogo" (ej: Alumno <-> Curso, donde
// ambos lados son el MISMO tipo de participante), aca los dos lados de la
// relacion son conceptualmente distintos aunque comparten la MISMA tabla de
// origen (Usuarios): un lado (ActivoDigital.UsuarioId, un salto INDIRECTO a
// traves de ActivoDigital) identifica al OTORGANTE, y el otro lado
// (AsignacionHerencia.UsuarioId, un salto DIRECTO) identifica al
// BENEFICIARIO. Es perfectamente valido, incluso esperable, que un mismo
// Usuario aparezca simultaneamente como otorgante de una fila y como
// beneficiario de otra: eso es exactamente lo que se siembra en
// AppDbContext (Usuario A le deja algo a Usuario B, y Usuario B le deja algo
// a Usuario C).
//
// La modelamos como una clase EXPLICITA (en vez de dejar que EF cree una
// tabla intermedia "oculta") porque necesitamos guardar datos PROPIOS de la
// relacion (el porcentaje, la condicion de liberacion y el estado de
// aceptacion) que no cabrian en una tabla puente automatica de solo 2 FKs.
public class AsignacionHerencia : EntidadBaseAuditable
{
    public int Id { get; set; }

    // --- FK hacia ActivoDigital (identifica, indirectamente via
    // ActivoDigital.UsuarioId, al OTORGANTE) ---
    public int ActivoDigitalId { get; set; }

    public ActivoDigital ActivoDigital { get; set; } = null!;

    // --- FK hacia Usuario, en su rol de BENEFICIARIO ---
    // Es NULLABLE (int?) a proposito: el titular puede invitar como
    // beneficiario a una persona por su Email SIN que esa persona todavia
    // tenga una cuenta creada en el sistema (ver EmailInvitado). Mientras esa
    // persona no se registre, esta columna queda en null; el dia que se
    // registra con ESE MISMO email, UsuarioService.CrearUsuarioAsync
    // "reclama" automaticamente esta fila y completa este campo con su
    // Id recien creado.
    public int? UsuarioId { get; set; }

    public Usuario? Usuario { get; set; }

    // Email con el que el otorgante invito a esta persona como beneficiaria.
    // Se completa SIEMPRE, tenga o no cuenta esa persona todavia, por dos
    // motivos:
    //  1) Si UsuarioId es null, es el UNICO dato de contacto disponible para
    //     enviarle la invitacion a crear una cuenta.
    //  2) Sirve de "llave de reclamo": el dia que alguien se registra con
    //     este mismo Email, el sistema vincula automaticamente esta fila a
    //     la cuenta recien creada (ver UsuarioService.CrearUsuarioAsync).
    public string EmailInvitado { get; set; } = string.Empty;

    // Porcentaje del activo que le corresponde a este beneficiario (0 a 100).
    // Se usa decimal (no float/double) porque es un valor monetario/proporcional
    // donde la precision exacta importa (evita errores de redondeo binario).
    public decimal PorcentajeAsignado { get; set; }

    // Condicion que debe cumplirse para liberar el activo hacia el beneficiario
    // (ej: "Certificado de defuncion + 30 dias sin objeciones", "Mayoria de edad").
    public string CondicionLiberacion { get; set; } = string.Empty;

    // Estado del flujo de aceptacion de ESTA asignacion puntual (ver el enum
    // EstadoBeneficiario para el detalle de por que se modela como enum).
    //
    // Vive ACA, en AsignacionHerencia, y no como un campo global del Usuario
    // beneficiario, porque un mismo Usuario puede recibir VARIAS herencias de
    // distintos otorgantes (o incluso varias del mismo), y debe poder aceptar
    // una y rechazar otra de forma completamente independiente: el estado de
    // aceptacion es un atributo de "esta relacion puntual otorgante-activo-
    // beneficiario", no de la persona beneficiaria en abstracto.
    public EstadoBeneficiario Estado { get; set; } = EstadoBeneficiario.Pendiente;

    // --- Identificador PUBLICO no adivinable de esta invitacion ---
    // TokenInvitacion es un valor aleatorio (generado en
    // AsignacionHerenciaService.CrearAsignacionesAsync) que identifica a esta
    // fila en los DOS endpoints PUBLICOS de InvitacionesController
    // (GET /api/invitaciones/{token} y POST /api/invitaciones/{token}/procesar),
    // en vez de usar directamente el "Id" (entero autoincremental) de la
    // fila. Se usa deliberadamente un valor NO SECUENCIAL y de alta entropia
    // para que nadie pueda "enumerar" invitaciones ajenas probando
    // 1, 2, 3... en la URL: esos dos endpoints no verifican ownership por
    // Token JWT (no lo necesitan: la propia persona invitada todavia podria
    // no tener cuenta), asi que la UNICA proteccion posible es que el
    // identificador en si sea imposible de adivinar. El Id entero interno
    // sigue existiendo y se sigue usando tal cual en el resto de la Api
    // (PATCH .../estado en AsignacionesController), que SI esta protegido
    // por [Authorize] + verificacion de ownership.
    public string TokenInvitacion { get; set; } = string.Empty;
}
