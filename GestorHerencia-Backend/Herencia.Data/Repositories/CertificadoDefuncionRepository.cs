using Herencia.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Herencia.Data.Repositories;

// CertificadoDefuncionRepository hereda el CRUD generico de
// RepositorioBase<CertificadoDefuncion> y suma las consultas especificas de
// la cola de revision.
public class CertificadoDefuncionRepository : RepositorioBase<CertificadoDefuncion>, ICertificadoDefuncionRepository
{
    public CertificadoDefuncionRepository(AppDbContext contexto) : base(contexto)
    {
    }

    public async Task<IEnumerable<CertificadoDefuncion>> ObtenerPendientesPorTitularAsync(int usuarioTitularId)
    {
        return await _contexto.CertificadosDefuncion
            .Where(c => c.UsuarioTitularId == usuarioTitularId && c.Estado == EstadoCertificadoDefuncion.Pendiente)
            .ToListAsync();
    }

    public async Task<IEnumerable<CertificadoDefuncion>> ObtenerPendientesAsync()
    {
        // Include de UsuarioTitular y SubidoPor: el panel de un
        // Administrador necesita mostrar nombre/email de ambos sin
        // consultas N+1 adicionales por cada fila de la cola.
        return await _contexto.CertificadosDefuncion
            .Include(c => c.UsuarioTitular)
            .Include(c => c.SubidoPor)
            .Where(c => c.Estado == EstadoCertificadoDefuncion.Pendiente)
            .ToListAsync();
    }

    public async Task<CertificadoDefuncion?> ObtenerConUsuariosAsync(int id)
    {
        return await _contexto.CertificadosDefuncion
            .Include(c => c.UsuarioTitular)
            .Include(c => c.SubidoPor)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<bool> ExisteCertificadoAprobadoAsync(int usuarioTitularId)
    {
        return await _contexto.CertificadosDefuncion
            .AnyAsync(c => c.UsuarioTitularId == usuarioTitularId && c.Estado == EstadoCertificadoDefuncion.Aprobado);
    }
}
