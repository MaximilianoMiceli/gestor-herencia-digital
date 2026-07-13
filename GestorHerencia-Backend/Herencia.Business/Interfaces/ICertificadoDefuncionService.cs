using Herencia.Business.Dtos;

namespace Herencia.Business.Interfaces;

/// <summary>Contrato de la logica de negocio de subida y revision de certificados de defuncion.</summary>
public interface ICertificadoDefuncionService
{
    /// <summary>
    /// Sube un nuevo certificado para el titular indicado. "contenidoArchivo" llega como
    /// Stream, ya abierto por el controller (ver IAlmacenamientoArchivosService).
    /// </summary>
    /// <exception cref="ReglaNegocioException">
    /// Tipo de archivo no permitido, tamaño excedido, o "subidoPorUsuarioId" no es un
    /// beneficiario aceptado del titular.
    /// </exception>
    /// <exception cref="RecursoNoEncontradoException">El titular indicado no existe.</exception>
    Task<CertificadoDefuncionDTO> SubirCertificadoAsync(
        int usuarioTitularId,
        int subidoPorUsuarioId,
        Stream contenidoArchivo,
        string nombreArchivoOriginal,
        string contentType,
        long tamanioBytes);

    /// <summary>Devuelve la cola completa de certificados Pendientes de revision (todos los titulares), para el panel de un Administrador.</summary>
    Task<IEnumerable<CertificadoDefuncionDTO>> ObtenerPendientesAsync();

    /// <summary>
    /// Aprueba un certificado Pendiente: confirma el fallecimiento del titular y libera
    /// los bienes hacia todos sus herederos ya aceptados (fija AsignacionHerencia.FechaLiberacion).
    /// </summary>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    /// <exception cref="ReglaNegocioException">El certificado ya habia sido revisado antes.</exception>
    Task<CertificadoDefuncionDTO> AprobarAsync(int certificadoId, int adminUsuarioId);

    /// <summary>
    /// Rechaza un certificado Pendiente con un motivo. No modifica el monitoreo de
    /// verificacion de vida: otro heredero puede volver a subir un certificado despues.
    /// </summary>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    /// <exception cref="ReglaNegocioException">El certificado ya habia sido revisado antes.</exception>
    Task<CertificadoDefuncionDTO> RechazarAsync(int certificadoId, int adminUsuarioId, string motivo);

    /// <summary>
    /// Devuelve la ruta en disco y el nombre original del archivo de un certificado, para
    /// que un Administrador pueda visualizarlo/descargarlo mientras lo revisa.
    /// </summary>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    Task<(string RutaArchivo, string NombreArchivoOriginal)> ObtenerArchivoAsync(int certificadoId);
}
