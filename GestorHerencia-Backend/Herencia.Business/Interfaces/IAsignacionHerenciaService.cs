using Herencia.Business.Dtos;
using Herencia.Data.Models;

namespace Herencia.Business.Interfaces;

/// <summary>
/// Contrato de la logica de negocio de AsignacionHerencia: la relacion N-N entre
/// ActivoDigital y Usuario Beneficiario, incluido el flujo de aceptacion/rechazo.
/// </summary>
public interface IAsignacionHerenciaService
{
    /// <summary>Devuelve todas las asignaciones de un ActivoDigital.</summary>
    /// <exception cref="RecursoNoEncontradoException">El activoDigitalId no existe.</exception>
    Task<IEnumerable<AsignacionHerenciaDTO>> ObtenerAsignacionesPorActivoAsync(int activoDigitalId);

    /// <summary>Busca una AsignacionHerencia por su Id (incluye el Id del otorgante, para ownership).</summary>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    Task<AsignacionHerenciaDTO> ObtenerAsignacionPorIdAsync(int id);

    /// <summary>Variante publica de <see cref="ObtenerAsignacionPorIdAsync"/> que busca por TokenInvitacion.</summary>
    /// <exception cref="RecursoNoEncontradoException">El token no existe.</exception>
    Task<AsignacionHerenciaDTO> ObtenerAsignacionPorTokenAsync(string token);

    /// <summary>Devuelve las asignaciones donde el Usuario participa como beneficiario ("mis herencias recibidas").</summary>
    Task<IEnumerable<AsignacionHerenciaDTO>> ObtenerAsignacionesPorUsuarioBeneficiarioAsync(int usuarioId);

    /// <summary>
    /// Da de alta un lote de asignaciones para un mismo ActivoDigital en una transaccion
    /// atomica. Si el email del beneficiario no tiene cuenta, queda como invitacion sin
    /// reclamar (UsuarioId null) hasta que se registre con ese email.
    /// </summary>
    /// <exception cref="RecursoNoEncontradoException">El activoDigitalId no existe.</exception>
    /// <exception cref="ReglaNegocioException">
    /// Porcentaje invalido, la suma supera el 100%, o auto-asignacion del otorgante.
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
    /// Pasa una asignacion de Pendiente a un estado final (Aceptado/Rechazado). Compartido
    /// por el PATCH autenticado (JWT) y el flujo publico por link de invitacion.
    /// </summary>
    /// <exception cref="RecursoNoEncontradoException">El asignacionId no existe.</exception>
    /// <exception cref="ReglaNegocioException">
    /// El estado actual ya es Aceptado/Rechazado, o "nuevoEstado" es Pendiente.
    /// </exception>
    Task<AsignacionHerenciaDTO> CambiarEstadoAsync(int asignacionId, EstadoBeneficiario nuevoEstado);

    /// <summary>Variante publica de <see cref="CambiarEstadoAsync"/> que identifica la asignacion por TokenInvitacion.</summary>
    Task<AsignacionHerenciaDTO> CambiarEstadoPorTokenAsync(string token, EstadoBeneficiario nuevoEstado);
}
