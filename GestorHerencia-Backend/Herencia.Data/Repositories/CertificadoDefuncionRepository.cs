using Herencia.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Herencia.Data.Repositories;

/// <summary>Repositorio de CertificadoDefuncion: hereda el CRUD genérico y suma las consultas de <see cref="ICertificadoDefuncionRepository"/>.</summary>
public class CertificadoDefuncionRepository : RepositorioBase<CertificadoDefuncion>, ICertificadoDefuncionRepository
{
    public CertificadoDefuncionRepository(AppDbContext contexto) : base(contexto)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CertificadoDefuncion>> ObtenerPendientesPorTitularAsync(int usuarioTitularId)
    {
        return await _contexto.CertificadosDefuncion
            .Where(c => c.UsuarioTitularId == usuarioTitularId && c.Estado == EstadoCertificadoDefuncion.Pendiente)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CertificadoDefuncion>> ObtenerPendientesAsync()
    {
        return await _contexto.CertificadosDefuncion
            .Include(c => c.UsuarioTitular)
            .Include(c => c.SubidoPor)
            .Where(c => c.Estado == EstadoCertificadoDefuncion.Pendiente)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CertificadoDefuncion?> ObtenerConUsuariosAsync(int id)
    {
        return await _contexto.CertificadosDefuncion
            .Include(c => c.UsuarioTitular)
            .Include(c => c.SubidoPor)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <inheritdoc />
    public async Task<bool> ExisteCertificadoAprobadoAsync(int usuarioTitularId)
    {
        return await _contexto.CertificadosDefuncion
            .AnyAsync(c => c.UsuarioTitularId == usuarioTitularId && c.Estado == EstadoCertificadoDefuncion.Aprobado);
    }
}
