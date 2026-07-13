using Herencia.Business.Dtos;

namespace Herencia.Business.Interfaces;

/// <summary>Contrato de la logica de negocio de subida y revision de certificados de defuncion.</summary>
public interface ICertificadoDefuncionService
{
    /// <summary>Sube un nuevo certificado para el titular indicado ("contenidoArchivo" ya viene como Stream abierto).</summary>
    /// <exception cref="ReglaNegocioException">
    /// Tipo de archivo no permitido, tamaño excedido, o "subidoPorUsuarioId" no es beneficiario aceptado.
    /// </exception>
    /// <exception cref="RecursoNoEncontradoException">El titular indicado no existe.</exception>
    Task<CertificadoDefuncionDTO> SubirCertificadoAsync(
        int usuarioTitularId,
        int subidoPorUsuarioId,
        Stream contenidoArchivo,
        string nombreArchivoOriginal,
        string contentType,
        long tamanioBytes);

    /// <summary>Devuelve la cola de certificados Pendientes de todos los titulares, para el panel del Administrador.</summary>
    Task<IEnumerable<CertificadoDefuncionDTO>> ObtenerPendientesAsync();

    /// <summary>Aprueba un certificado Pendiente: confirma el fallecimiento y libera los bienes a los herederos aceptados.</summary>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    /// <exception cref="ReglaNegocioException">El certificado ya habia sido revisado antes.</exception>
    Task<CertificadoDefuncionDTO> AprobarAsync(int certificadoId, int adminUsuarioId);

    /// <summary>Rechaza un certificado Pendiente con un motivo; otro heredero puede volver a subir uno despues.</summary>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    /// <exception cref="ReglaNegocioException">El certificado ya habia sido revisado antes.</exception>
    Task<CertificadoDefuncionDTO> RechazarAsync(int certificadoId, int adminUsuarioId, string motivo);

    /// <summary>Devuelve la ruta en disco y el nombre original del archivo de un certificado.</summary>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    Task<(string RutaArchivo, string NombreArchivoOriginal)> ObtenerArchivoAsync(int certificadoId);
}
