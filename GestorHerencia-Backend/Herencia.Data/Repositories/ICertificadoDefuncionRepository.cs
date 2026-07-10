using Herencia.Data.Models;

namespace Herencia.Data.Repositories;

// ICertificadoDefuncionRepository extiende el contrato generico
// IRepositorioBase<CertificadoDefuncion> para sumar las consultas propias
// del flujo de revision de certificados.
public interface ICertificadoDefuncionRepository : IRepositorioBase<CertificadoDefuncion>
{
    // Devuelve los certificados PENDIENTES de un titular puntual. Se usa
    // (a) en VerificacionVidaService.RegistrarCheckInAsync, para saber si
    // hay que auto-cancelarlos cuando el titular vuelve a confirmar
    // actividad, y (b) en CertificadoDefuncionService, para decidir si ya
    // existe una revision en curso antes de crear una nueva fila.
    Task<IEnumerable<CertificadoDefuncion>> ObtenerPendientesPorTitularAsync(int usuarioTitularId);

    // Devuelve TODOS los certificados Pendientes de TODOS los titulares:
    // es la cola de revision que consume el panel de un Administrador
    // (GET /api/certificados-defuncion/pendientes).
    Task<IEnumerable<CertificadoDefuncion>> ObtenerPendientesAsync();

    // Busca un certificado por Id, con UsuarioTitular y SubidoPor ya
    // cargados (Include): lo necesitan Aprobar/Rechazar para poder devolver
    // el DTO completo (con los nombres resueltos) sin una consulta extra.
    Task<CertificadoDefuncion?> ObtenerConUsuariosAsync(int id);
}
