using Herencia.Business.Dtos;
using Herencia.Data.Models;

namespace Herencia.Business.Interfaces;

/// <summary>
/// Contrato de la logica de negocio de ActivoDigital. Trabaja exclusivamente
/// con DTOs para que la capa Api dependa de esta interfaz, nunca de la
/// implementacion concreta ni de las entidades de Data.
/// </summary>
public interface IActivoDigitalService
{
    /// <summary>
    /// Da de alta un nuevo ActivoDigital para el Usuario titular indicado en el DTO.
    /// </summary>
    /// <exception cref="ReglaNegocioException">Nombre vacio o error tecnico al persistir.</exception>
    /// <exception cref="RecursoNoEncontradoException">El UsuarioId no corresponde a ningun Usuario.</exception>
    Task<ActivoDigitalDTO> CrearActivoDigitalAsync(ActivoDigitalCreacionDTO activoDigitalCreacionDTO);

    /// <summary>Busca un ActivoDigital por su Id.</summary>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    /// <exception cref="ReglaNegocioException">Error tecnico al consultarlo.</exception>
    Task<ActivoDigitalDTO> ObtenerActivoDigitalPorIdAsync(int id);

    /// <summary>Devuelve todos los ActivosDigitales de un Usuario.</summary>
    /// <exception cref="RecursoNoEncontradoException">El usuarioId no existe.</exception>
    /// <exception cref="ReglaNegocioException">Error tecnico al consultarlos.</exception>
    Task<IEnumerable<ActivoDigitalDTO>> ObtenerActivosPorUsuarioAsync(int usuarioId);

    /// <summary>
    /// Actualiza Nombre, Tipo y Descripcion de un ActivoDigital existente. No permite
    /// reasignar el Usuario titular (ver ActivoDigitalActualizacionDTO).
    /// </summary>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    /// <exception cref="ReglaNegocioException">Datos invalidos o error tecnico al persistir.</exception>
    Task<ActivoDigitalDTO> ActualizarActivoDigitalAsync(int id, ActivoDigitalActualizacionDTO activoDigitalActualizacionDTO);

    /// <summary>
    /// Elimina un ActivoDigital existente (y, por cascada en AppDbContext, sus
    /// AsignacionesHerencia asociadas).
    /// </summary>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    /// <exception cref="ReglaNegocioException">Error tecnico al eliminarlo.</exception>
    Task EliminarActivoDigitalAsync(int id);

    /// <summary>
    /// Version paginada y filtrada de <see cref="ObtenerActivosPorUsuarioAsync"/>: devuelve
    /// solo la pagina pedida junto con el total de registros/paginas. "tipo" y "nombre" son
    /// filtros opcionales (categoria exacta y/o texto parcial del nombre).
    /// </summary>
    /// <param name="usuarioId">
    /// Siempre proviene del Claim del Token JWT del usuario autenticado, nunca de un
    /// parametro que el cliente pueda manipular libremente.
    /// </param>
    /// <exception cref="RecursoNoEncontradoException">El usuarioId no existe.</exception>
    /// <exception cref="ReglaNegocioException">Error tecnico al consultarlos.</exception>
    Task<ResultadoPaginadoDTO<ActivoDigitalDTO>> ObtenerActivosPorUsuarioPaginadoAsync(
        int usuarioId, int pagina, int limite, TipoActivoDigital? tipo, string? nombre);

    /// <summary>
    /// Adjunta (o reemplaza) el archivo asociado a un ActivoDigital existente.
    /// Recibe un Stream + metadatos en vez de IFormFile porque Business no referencia
    /// el framework web (ver IAlmacenamientoArchivosService).
    /// </summary>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    /// <exception cref="ReglaNegocioException">
    /// Tipo de archivo no permitido, tamaño excedido, o error tecnico al guardarlo.
    /// </exception>
    Task<ActivoDigitalDTO> SubirArchivoAsync(
        int id, Stream contenido, string nombreArchivoOriginal, string contentType, long tamanioBytes);

    /// <summary>
    /// Devuelve la ruta en disco y el nombre original del archivo adjunto de un ActivoDigital.
    /// La verificacion de quien puede pedirlo vive en el controller, no aca: este metodo solo
    /// resuelve donde esta guardado el archivo.
    /// </summary>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    Task<(string RutaArchivo, string NombreArchivoOriginal)> ObtenerArchivoAsync(int id);
}
