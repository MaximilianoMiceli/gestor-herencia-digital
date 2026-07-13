using Herencia.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Herencia.Data.Repositories;

/// <summary>Repositorio de CertificadoDefuncion: hereda el CRUD genérico y suma las consultas de <see cref="ICertificadoDefuncionRepository"/>.</summary>
public class CertificadoDefuncionRepository : RepositorioBase<CertificadoDefuncion>, ICertificadoDefuncionRepository
{
    public CertificadoDefuncionRepository(AppDbContext contexto) : base(contexto)
    {
    }

    /// <summary>Ver <see cref="ICertificadoDefuncionRepository.ObtenerPendientesPorTitularAsync"/>.</summary>
    public async Task<IEnumerable<CertificadoDefuncion>> ObtenerPendientesPorTitularAsync(int usuarioTitularId)
    {
        return await _contexto.CertificadosDefuncion
            .Where(c => c.UsuarioTitularId == usuarioTitularId && c.Estado == EstadoCertificadoDefuncion.Pendiente)
            .ToListAsync();
    }

    /// <summary>Ver <see cref="ICertificadoDefuncionRepository.ObtenerPendientesAsync"/>.</summary>
    public async Task<IEnumerable<CertificadoDefuncion>> ObtenerPendientesAsync()
    {
        // Include de UsuarioTitular y SubidoPor: el panel de Administrador necesita mostrar
        // nombre/email de ambos sin consultas N+1 por cada fila.
        return await _contexto.CertificadosDefuncion
            .Include(c => c.UsuarioTitular)
            .Include(c => c.SubidoPor)
            .Where(c => c.Estado == EstadoCertificadoDefuncion.Pendiente)
            .ToListAsync();
    }

    /// <summary>Ver <see cref="ICertificadoDefuncionRepository.ObtenerConUsuariosAsync"/>.</summary>
    public async Task<CertificadoDefuncion?> ObtenerConUsuariosAsync(int id)
    {
        return await _contexto.CertificadosDefuncion
            .Include(c => c.UsuarioTitular)
            .Include(c => c.SubidoPor)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <summary>Ver <see cref="ICertificadoDefuncionRepository.ExisteCertificadoAprobadoAsync"/>.</summary>
    public async Task<bool> ExisteCertificadoAprobadoAsync(int usuarioTitularId)
    {
        return await _contexto.CertificadosDefuncion
            .AnyAsync(c => c.UsuarioTitularId == usuarioTitularId && c.Estado == EstadoCertificadoDefuncion.Aprobado);
    }
}
