using Herencia.Business.Dtos;

namespace Herencia.Business.Interfaces;

// ICertificadoDefuncionService es el CONTRATO publico de la logica de
// negocio de subida y revision de certificados de defuncion.
public interface ICertificadoDefuncionService
{
    // Sube un nuevo certificado para el titular indicado. "contenidoArchivo"
    // llega como Stream (no como IFormFile: ver el comentario de
    // IAlmacenamientoArchivosService) ya abierto por el controller.
    // Puede lanzar:
    //  - ReglaNegocioException: tipo de archivo no permitido, tamaño
    //    excedido, o "subidoPorUsuarioId" no es un beneficiario aceptado
    //    del titular.
    //  - RecursoNoEncontradoException: el titular indicado no existe.
    Task<CertificadoDefuncionDTO> SubirCertificadoAsync(
        int usuarioTitularId,
        int subidoPorUsuarioId,
        Stream contenidoArchivo,
        string nombreArchivoOriginal,
        string contentType,
        long tamanioBytes);

    // Devuelve la cola completa de certificados Pendientes de revision
    // (todos los titulares), para el panel de un Administrador.
    Task<IEnumerable<CertificadoDefuncionDTO>> ObtenerPendientesAsync();

    // Aprueba un certificado Pendiente: confirma el fallecimiento del
    // titular y LIBERA los bienes hacia todos sus herederos ya aceptados
    // (fija AsignacionHerencia.FechaLiberacion). Puede lanzar
    // RecursoNoEncontradoException (el Id no existe) o ReglaNegocioException
    // (el certificado ya habia sido revisado antes).
    Task<CertificadoDefuncionDTO> AprobarAsync(int certificadoId, int adminUsuarioId);

    // Rechaza un certificado Pendiente con un motivo. No modifica el estado
    // del monitoreo de verificacion de vida: otro heredero puede volver a
    // subir un certificado despues. Mismas excepciones que AprobarAsync.
    Task<CertificadoDefuncionDTO> RechazarAsync(int certificadoId, int adminUsuarioId, string motivo);

    // Devuelve la ruta en disco y el nombre original del archivo de un
    // certificado, para que un Administrador pueda visualizarlo/descargarlo
    // mientras lo revisa (antes solo se le mostraban los metadatos: titular,
    // quien lo subio, fecha). Lanza RecursoNoEncontradoException si el Id no
    // existe.
    Task<(string RutaArchivo, string NombreArchivoOriginal)> ObtenerArchivoAsync(int certificadoId);
}
