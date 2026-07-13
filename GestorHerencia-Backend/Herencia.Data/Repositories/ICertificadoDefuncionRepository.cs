using Herencia.Data.Models;

namespace Herencia.Data.Repositories;

/// <summary>Extiende el CRUD genérico con las consultas propias del flujo de revisión de certificados.</summary>
public interface ICertificadoDefuncionRepository : IRepositorioBase<CertificadoDefuncion>
{
    /// <summary>Devuelve los certificados Pendientes de un titular (usado para auto-cancelar en check-in y evitar duplicados).</summary>
    Task<IEnumerable<CertificadoDefuncion>> ObtenerPendientesPorTitularAsync(int usuarioTitularId);

    /// <summary>Devuelve todos los certificados Pendientes: la cola de revisión del panel de Administrador.</summary>
    Task<IEnumerable<CertificadoDefuncion>> ObtenerPendientesAsync();

    /// <summary>Busca un certificado por Id con UsuarioTitular y SubidoPor cargados (Include).</summary>
    Task<CertificadoDefuncion?> ObtenerConUsuariosAsync(int id);

    /// <summary>True si ya existe un certificado Aprobado para este titular (evita nuevas subidas tras la liberación).</summary>
    Task<bool> ExisteCertificadoAprobadoAsync(int usuarioTitularId);
}
