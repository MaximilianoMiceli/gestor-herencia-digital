using Herencia.Data.Models;

namespace Herencia.Data.Repositories;

/// <summary>
/// Extiende el CRUD genérico (incluido EjecutarEnTransaccionAsync) con las consultas propias
/// de la relación N-N entre ActivoDigital y Usuario (en su rol de Beneficiario).
/// </summary>
public interface IAsignacionHerenciaRepository : IRepositorioBase<AsignacionHerencia>
{
    /// <summary>Devuelve todas las AsignacionHerencia de un ActivoDigital puntual.</summary>
    Task<IEnumerable<AsignacionHerencia>> ObtenerPorActivoDigitalAsync(int activoDigitalId);

    /// <summary>
    /// Busca una AsignacionHerencia por Id con su ActivoDigital cargado (Include), necesario
    /// para resolver en el controller si el usuario autenticado es el titular (otorgante) del
    /// activo al que pertenece la asignación.
    /// </summary>
    Task<AsignacionHerencia?> ObtenerConActivoDigitalAsync(int id);

    /// <summary>
    /// Devuelve las AsignacionHerencia donde el Usuario participa como Beneficiario, con su
    /// ActivoDigital cargado (Include). Consulta detrás de GET /api/invitaciones/mis-herencias.
    /// </summary>
    Task<IEnumerable<AsignacionHerencia>> ObtenerPorUsuarioBeneficiarioAsync(int usuarioId);

    /// <summary>
    /// Devuelve las AsignacionHerencia sin reclamar (UsuarioId null) cuyo EmailInvitado coincide
    /// con el email indicado, sin distinguir mayúsculas/minúsculas. La usa
    /// UsuarioService.CrearUsuarioAsync para vincular invitaciones pendientes al registrarse.
    /// </summary>
    Task<IEnumerable<AsignacionHerencia>> ObtenerPendientesPorEmailAsync(string email);

    /// <summary>
    /// Busca una AsignacionHerencia por su TokenInvitacion (identificador público no adivinable),
    /// con su ActivoDigital cargado. La usa InvitacionesController, que expone esta entidad sin
    /// exigir JWT.
    /// </summary>
    Task<AsignacionHerencia?> ObtenerPorTokenInvitacionAsync(string token);

    /// <summary>
    /// Devuelve las AsignacionHerencia ya Aceptadas cuyo ActivoDigital pertenece al Usuario
    /// indicado como otorgante ("los herederos aceptados de este titular"), con su Usuario
    /// beneficiario cargado. La usan VerificacionVidaService.EjecutarEscaneoAsync (para notificar
    /// al activarse el protocolo) y CertificadoDefuncionService (para validar quién puede subir
    /// un certificado y para liberar los bienes al aprobarse). Se resuelve con un JOIN real
    /// (vía ActivoDigital.UsuarioId), no con un bucle N+1, porque se reutiliza en varios puntos.
    /// </summary>
    Task<IEnumerable<AsignacionHerencia>> ObtenerAceptadasPorOtorganteAsync(int usuarioOtorganteId);
}
