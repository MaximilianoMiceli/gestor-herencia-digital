using Herencia.Business.Dtos;
using Herencia.Data.Models;

namespace Herencia.Business.Interfaces;

/// <summary>
/// Contrato de la logica de negocio de AsignacionHerencia: la relacion N-N entre
/// ActivoDigital y Usuario (en su rol de Beneficiario). Absorbe tambien el flujo de
/// aceptacion/rechazo, porque el Estado es un atributo de cada asignacion puntual y no
/// de una entidad Beneficiario aparte.
/// </summary>
public interface IAsignacionHerenciaService
{
    /// <summary>Devuelve todas las asignaciones de un ActivoDigital.</summary>
    /// <exception cref="RecursoNoEncontradoException">El activoDigitalId no existe.</exception>
    Task<IEnumerable<AsignacionHerenciaDTO>> ObtenerAsignacionesPorActivoAsync(int activoDigitalId);

    /// <summary>
    /// Busca una AsignacionHerencia por su Id (incluye el Id del Usuario otorgante, para
    /// que la capa Api resuelva la verificacion de ownership).
    /// </summary>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    Task<AsignacionHerenciaDTO> ObtenerAsignacionPorIdAsync(int id);

    /// <summary>
    /// Variante publica de <see cref="ObtenerAsignacionPorIdAsync"/>: busca por
    /// TokenInvitacion (identificador no adivinable expuesto via InvitacionesController)
    /// en vez de por el Id entero interno.
    /// </summary>
    /// <exception cref="RecursoNoEncontradoException">El token no existe.</exception>
    Task<AsignacionHerenciaDTO> ObtenerAsignacionPorTokenAsync(string token);

    /// <summary>Devuelve las asignaciones donde el Usuario participa como beneficiario ("mis herencias recibidas").</summary>
    Task<IEnumerable<AsignacionHerenciaDTO>> ObtenerAsignacionesPorUsuarioBeneficiarioAsync(int usuarioId);

    /// <summary>
    /// Da de alta un lote de asignaciones para un mismo ActivoDigital dentro de una unica
    /// transaccion atomica: si cualquier item del lote es invalido, ninguna queda persistida.
    /// Para cada item se busca un Usuario existente por EmailBeneficiario: si existe, la
    /// asignacion queda vinculada a esa cuenta con Estado Pendiente; si no, queda con
    /// UsuarioId null (invitacion sin reclamar) hasta que esa persona se registre con el
    /// mismo email (ver UsuarioService.CrearUsuarioAsync).
    /// </summary>
    /// <exception cref="RecursoNoEncontradoException">El activoDigitalId no existe.</exception>
    /// <exception cref="ReglaNegocioException">
    /// Porcentaje invalido, la suma de porcentajes (existentes + nuevos) supera el 100%,
    /// el otorgante intenta asignarse el activo a si mismo, o error tecnico al persistir.
    /// </exception>
    Task<IEnumerable<AsignacionHerenciaDTO>> CrearAsignacionesAsync(
        int activoDigitalId,
        IEnumerable<AsignacionHerenciaCreacionDTO> asignacionesCreacionDTO);

    /// <summary>
    /// Actualiza Porcentaje/Condicion de una asignacion existente, validando que el nuevo
    /// porcentaje no haga superar el 100% acumulado del ActivoDigital.
    /// </summary>
    Task<AsignacionHerenciaDTO> ActualizarAsignacionAsync(int id, AsignacionHerenciaActualizacionDTO asignacionActualizacionDTO);

    /// <summary>Elimina una asignacion existente.</summary>
    Task EliminarAsignacionAsync(int id);

    /// <summary>
    /// Implementa la aceptacion/rechazo de la herencia digital, pasando el estado de
    /// Pendiente a un estado final (Aceptado o Rechazado). Metodo compartido por dos
    /// puntos de entrada con modelos de confianza distintos: el PATCH autenticado de
    /// AsignacionesController (exige que el llamador sea el beneficiario, via JWT) y el
    /// flujo publico de InvitacionesController (confia en quien conoce el link recibido
    /// por email). Por eso no exige que "asignacionId" ya este vinculado a un Usuario;
    /// esa exigencia, cuando corresponde, la aplica el controller.
    /// </summary>
    /// <exception cref="RecursoNoEncontradoException">El asignacionId no existe.</exception>
    /// <exception cref="ReglaNegocioException">
    /// El estado actual ya es Aceptado/Rechazado (decision final, no revisable), o
    /// "nuevoEstado" es Pendiente (estado que solo el sistema asigna automaticamente).
    /// </exception>
    Task<AsignacionHerenciaDTO> CambiarEstadoAsync(int asignacionId, EstadoBeneficiario nuevoEstado);

    /// <summary>
    /// Variante publica de <see cref="CambiarEstadoAsync"/> que identifica la asignacion por
    /// TokenInvitacion en vez de Id interno. La usa InvitacionesController.ProcesarInvitacion
    /// (sin login). Mismas reglas y excepciones que CambiarEstadoAsync.
    /// </summary>
    Task<AsignacionHerenciaDTO> CambiarEstadoPorTokenAsync(string token, EstadoBeneficiario nuevoEstado);
}
