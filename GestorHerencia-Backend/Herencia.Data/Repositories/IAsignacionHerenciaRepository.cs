using Herencia.Data.Models;

namespace Herencia.Data.Repositories;

// IAsignacionHerenciaRepository extiende el contrato generico
// IRepositorioBase<AsignacionHerencia> (que ya incluye
// EjecutarEnTransaccionAsync) para sumar consultas propias de la relacion
// N-N entre ActivoDigital y Usuario (en su rol de Beneficiario).
public interface IAsignacionHerenciaRepository : IRepositorioBase<AsignacionHerencia>
{
    // Devuelve todas las AsignacionHerencia de un ActivoDigital puntual (el
    // "detalle" de la relacion maestro-detalle ActivoDigital -> AsignacionHerencia).
    Task<IEnumerable<AsignacionHerencia>> ObtenerPorActivoDigitalAsync(int activoDigitalId);

    // Busca una unica AsignacionHerencia por su Id, trayendo tambien de forma
    // ANSIOSA (Eager Loading) su ActivoDigital relacionado. Se necesita el
    // ActivoDigital (y, a traves de el, su UsuarioId) para poder resolver la
    // verificacion de OWNERSHIP en el controller: "¿el usuario autenticado es
    // el titular (otorgante) del ActivoDigital al que pertenece esta asignacion?".
    Task<AsignacionHerencia?> ObtenerConActivoDigitalAsync(int id);

    // Devuelve todas las AsignacionHerencia en las que el Usuario indicado
    // participa como BENEFICIARIO (UsuarioId == usuarioId), trayendo de forma
    // ansiosa su ActivoDigital relacionado (para poder mostrar, por ejemplo,
    // el nombre/tipo del activo heredado sin una consulta adicional). Es la
    // consulta detras de "GET /api/invitaciones/mis-herencias".
    Task<IEnumerable<AsignacionHerencia>> ObtenerPorUsuarioBeneficiarioAsync(int usuarioId);

    // Devuelve todas las AsignacionHerencia todavia SIN reclamar (UsuarioId
    // es null) cuyo EmailInvitado coincide con el email indicado, sin
    // distinguir mayusculas/minusculas. La usa
    // UsuarioService.CrearUsuarioAsync para, apenas alguien se registra,
    // vincularle automaticamente cualquier invitacion pendiente que lo
    // nombraba por ese mismo email.
    Task<IEnumerable<AsignacionHerencia>> ObtenerPendientesPorEmailAsync(string email);

    // Busca una unica AsignacionHerencia por su TokenInvitacion (el
    // identificador PUBLICO no adivinable, ver el comentario de esa
    // propiedad), trayendo tambien de forma ansiosa su ActivoDigital
    // relacionado. La usa InvitacionesController, que expone esta entidad
    // por Internet sin exigir un Token JWT.
    Task<AsignacionHerencia?> ObtenerPorTokenInvitacionAsync(string token);

    // Devuelve todas las AsignacionHerencia YA ACEPTADAS cuyo ActivoDigital
    // pertenece al Usuario indicado en su rol de OTORGANTE (es decir, "los
    // herederos aceptados de este titular"), con su Usuario beneficiario ya
    // cargado (Include). La usan VerificacionVidaService.EjecutarEscaneoAsync
    // (para notificar a los herederos al activarse el protocolo) y
    // CertificadoDefuncionService (para validar que quien sube un
    // certificado sea uno de estos herederos, y para liberar los bienes de
    // TODOS ellos al aprobarse el certificado). Se resuelve con un JOIN real
    // (via la propiedad de navegacion ActivoDigital.UsuarioId), no con un
    // bucle N+1 sobre cada ActivoDigital del titular, porque este mismo
    // resultado se reutiliza en tres puntos distintos del dominio.
    Task<IEnumerable<AsignacionHerencia>> ObtenerAceptadasPorOtorganteAsync(int usuarioOtorganteId);
}
