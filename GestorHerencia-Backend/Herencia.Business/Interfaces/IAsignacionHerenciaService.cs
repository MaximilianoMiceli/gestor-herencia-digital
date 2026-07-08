using Herencia.Business.Dtos;

namespace Herencia.Business.Interfaces;

// IAsignacionHerenciaService es el CONTRATO publico de la logica de negocio
// de AsignacionHerencia: la relacion N-N entre ActivoDigital y Beneficiario.
public interface IAsignacionHerenciaService
{
    // Devuelve todas las asignaciones de un ActivoDigital puntual (el
    // "detalle" de la relacion maestro-detalle ActivoDigital -> AsignacionHerencia).
    // Puede lanzar RecursoNoEncontradoException si el activoDigitalId no existe.
    Task<IEnumerable<AsignacionHerenciaDTO>> ObtenerAsignacionesPorActivoAsync(int activoDigitalId);

    // Busca una unica AsignacionHerencia por su Id (incluye el UsuarioId del
    // titular del ActivoDigital relacionado, para que la capa Api pueda
    // resolver la verificacion de ownership).
    // Puede lanzar RecursoNoEncontradoException si el Id no existe.
    Task<AsignacionHerenciaDTO> ObtenerAsignacionPorIdAsync(int id);

    // CrearAsignacionesAsync: da de alta un LOTE de asignaciones para un
    // mismo ActivoDigital, TODAS dentro de una unica transaccion atomica
    // (ver RepositorioBase.EjecutarEnTransaccionAsync): si CUALQUIER
    // asignacion del lote es invalida (beneficiario inexistente, de otro
    // titular, o el porcentaje acumulado supera el 100%), NINGUNA de las
    // asignaciones del lote queda persistida, ni siquiera las que se
    // hubieran procesado con exito ANTES de la que fallo.
    // Puede lanzar:
    //  - RecursoNoEncontradoException: si el activoDigitalId, o algun
    //    BeneficiarioId del lote, no existen.
    //  - ReglaNegocioException: si algun porcentaje es invalido, si la suma
    //    de porcentajes (existentes + nuevos) supera el 100%, si algun
    //    beneficiario pertenece a un titular distinto al del activo, o si
    //    ocurre un error tecnico al persistir.
    Task<IEnumerable<AsignacionHerenciaDTO>> CrearAsignacionesAsync(
        int activoDigitalId,
        IEnumerable<AsignacionHerenciaCreacionDTO> asignacionesCreacionDTO);

    // Actualiza el Porcentaje/Condicion de una unica asignacion existente,
    // validando que el nuevo porcentaje (reemplazando el actual de esa misma
    // asignacion) no haga superar el 100% acumulado del ActivoDigital.
    Task<AsignacionHerenciaDTO> ActualizarAsignacionAsync(int id, AsignacionHerenciaActualizacionDTO asignacionActualizacionDTO);

    // Elimina una asignacion existente.
    Task EliminarAsignacionAsync(int id);
}
