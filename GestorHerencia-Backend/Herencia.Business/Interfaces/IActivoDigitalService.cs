using Herencia.Business.Dtos;
using Herencia.Data.Models;

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

    // Version PAGINADA y FILTRADA de ObtenerActivosPorUsuarioAsync: en vez de
    // devolver TODOS los activos de un usuario de una sola vez, devuelve
    // solo la "pagina" pedida (parametros "pagina" y "limite"), envuelta en
    // un ResultadoPaginadoDTO que ademas informa el total de registros/
    // paginas. Los parametros "tipo" y "nombre" son FILTROS OPCIONALES
    // (nullable): si ambos vienen null, se comporta igual que sin filtrar;
    // si se envian, restringen la busqueda por categoria y/o por texto
    // parcial del nombre. Es el metodo que consume el endpoint
    // "GET /api/activos" (protegido con [Authorize]), donde el usuarioId
    // SIEMPRE proviene del Claim del Token JWT del usuario autenticado,
    // nunca de un parametro que el cliente pueda manipular libremente.
    // Puede lanzar RecursoNoEncontradoException si el usuarioId no existe, o
    // ReglaNegocioException si ocurre un error tecnico al consultarlos.
    Task<ResultadoPaginadoDTO<ActivoDigitalDTO>> ObtenerActivosPorUsuarioPaginadoAsync(
        int usuarioId, int pagina, int limite, TipoActivoDigital? tipo, string? nombre);

    // SubirArchivoAsync: adjunta (o reemplaza) el archivo asociado a un
    // ActivoDigital existente (ej: el escaneo de un contrato, un PDF con
    // instrucciones notariales). Recibe un Stream + metadatos en vez de
    // "IFormFile" por el mismo motivo que ICertificadoDefuncionService: esta
    // libreria de Business no referencia el framework web (ver el comentario
    // de IAlmacenamientoArchivosService).
    // Puede lanzar:
    //  - RecursoNoEncontradoException: si el Id no existe.
    //  - ReglaNegocioException: si el tipo de archivo no esta permitido, si
    //    supera el tamaño maximo, o si ocurre un error tecnico al guardarlo.
    Task<ActivoDigitalDTO> SubirArchivoAsync(
        int id, Stream contenido, string nombreArchivoOriginal, string contentType, long tamanioBytes);
}
