using Herencia.Business.Dtos;

namespace Herencia.Business.Interfaces;

// IActivoDigitalService es el CONTRATO publico de la logica de negocio
// relacionada a ActivoDigital. Mismo criterio que IUsuarioService: trabaja
// solo con DTOs, para que la futura capa Api dependa unicamente de esta
// interfaz y no de la implementacion concreta ni de las entidades de Data.
public interface IActivoDigitalService
{
    // Da de alta un nuevo ActivoDigital para un Usuario titular puntual
    // (dto.UsuarioId). Antes de crear el activo, el servicio valida que ese
    // Usuario realmente exista (regla de negocio explicita de la rubrica).
    // Puede lanzar:
    //  - ReglaNegocioException: si el Nombre del activo viene vacio, o si
    //    ocurre un error tecnico al persistirlo.
    //  - RecursoNoEncontradoException: si el UsuarioId indicado no corresponde
    //    a ningun Usuario existente.
    Task<ActivoDigitalDTO> CrearActivoDigitalAsync(ActivoDigitalCreacionDTO activoDigitalCreacionDTO);

    // Busca un unico ActivoDigital por su Id.
    // Puede lanzar RecursoNoEncontradoException si el Id no existe, o
    // ReglaNegocioException si ocurre un error tecnico al consultarlo.
    Task<ActivoDigitalDTO> ObtenerActivoDigitalPorIdAsync(int id);

    // Devuelve todos los ActivosDigitales que pertenecen a un Usuario puntual.
    // Puede lanzar RecursoNoEncontradoException si el usuarioId no existe, o
    // ReglaNegocioException si ocurre un error tecnico al consultarlos.
    Task<IEnumerable<ActivoDigitalDTO>> ObtenerActivosPorUsuarioAsync(int usuarioId);

    // Actualiza el Nombre, Tipo y Descripcion de un ActivoDigital existente.
    // No permite reasignar el Usuario titular (ver comentario en
    // ActivoDigitalActualizacionDTO). Puede lanzar RecursoNoEncontradoException
    // si el Id no existe, o ReglaNegocioException si los nuevos datos son
    // invalidos o si ocurre un error tecnico al persistir el cambio.
    Task<ActivoDigitalDTO> ActualizarActivoDigitalAsync(int id, ActivoDigitalActualizacionDTO activoDigitalActualizacionDTO);

    // Elimina un ActivoDigital existente (y, por la configuracion de cascada del
    // AppDbContext, tambien sus AsignacionesHerencia asociadas).
    // Puede lanzar RecursoNoEncontradoException si el Id no existe, o
    // ReglaNegocioException si ocurre un error tecnico al eliminarlo.
    Task EliminarActivoDigitalAsync(int id);
}
