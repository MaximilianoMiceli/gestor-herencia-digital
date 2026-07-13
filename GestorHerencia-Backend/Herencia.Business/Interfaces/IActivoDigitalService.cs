using Herencia.Business.Dtos;
using Herencia.Data.Models;

namespace Herencia.Business.Interfaces;

/// <summary>
/// Contrato de la logica de negocio de ActivoDigital. Trabaja solo con DTOs para que
/// la capa Api nunca dependa de las entidades de Data.
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

    /// <summary>Actualiza Nombre, Tipo y Descripcion; no permite reasignar el Usuario titular.</summary>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    /// <exception cref="ReglaNegocioException">Datos invalidos o error tecnico al persistir.</exception>
    Task<ActivoDigitalDTO> ActualizarActivoDigitalAsync(int id, ActivoDigitalActualizacionDTO activoDigitalActualizacionDTO);

    /// <summary>Elimina un ActivoDigital existente (cascade borra sus AsignacionesHerencia).</summary>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    /// <exception cref="ReglaNegocioException">Error tecnico al eliminarlo.</exception>
    Task EliminarActivoDigitalAsync(int id);

    /// <summary>
    /// Version paginada y filtrada de <see cref="ObtenerActivosPorUsuarioAsync"/>, con
    /// "tipo" y "nombre" como filtros opcionales.
    /// </summary>
    /// <exception cref="RecursoNoEncontradoException">El usuarioId no existe.</exception>
    /// <exception cref="ReglaNegocioException">Error tecnico al consultarlos.</exception>
    Task<ResultadoPaginadoDTO<ActivoDigitalDTO>> ObtenerActivosPorUsuarioPaginadoAsync(
        int usuarioId, int pagina, int limite, TipoActivoDigital? tipo, string? nombre);

    /// <summary>
    /// Adjunta (o reemplaza) el archivo de un ActivoDigital. Recibe Stream + metadatos
    /// en vez de IFormFile porque Business no referencia el framework web.
    /// </summary>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    /// <exception cref="ReglaNegocioException">Tipo de archivo no permitido o tamaño excedido.</exception>
    Task<ActivoDigitalDTO> SubirArchivoAsync(
        int id, Stream contenido, string nombreArchivoOriginal, string contentType, long tamanioBytes);

    /// <summary>
    /// Devuelve la ruta en disco y el nombre original del archivo. La verificacion de
    /// quien puede pedirlo vive en el controller, no aca.
    /// </summary>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    Task<(string RutaArchivo, string NombreArchivoOriginal)> ObtenerArchivoAsync(int id);
}
