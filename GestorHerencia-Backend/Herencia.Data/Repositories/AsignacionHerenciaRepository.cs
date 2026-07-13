using Herencia.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Herencia.Data.Repositories;

/// <summary>Repositorio de AsignacionHerencia: hereda el CRUD genérico y suma las consultas de <see cref="IAsignacionHerenciaRepository"/>.</summary>
public class AsignacionHerenciaRepository : RepositorioBase<AsignacionHerencia>, IAsignacionHerenciaRepository
{
    public AsignacionHerenciaRepository(AppDbContext contexto) : base(contexto)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AsignacionHerencia>> ObtenerPorActivoDigitalAsync(int activoDigitalId)
    {
        return await _contexto.AsignacionesHerencia
            .Where(a => a.ActivoDigitalId == activoDigitalId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<AsignacionHerencia?> ObtenerConActivoDigitalAsync(int id)
    {
        return await _contexto.AsignacionesHerencia
            .Include(a => a.ActivoDigital)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AsignacionHerencia>> ObtenerPorUsuarioBeneficiarioAsync(int usuarioId)
    {
        return await _contexto.AsignacionesHerencia
            .Include(a => a.ActivoDigital)
            .Where(a => a.UsuarioId == usuarioId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AsignacionHerencia>> ObtenerPendientesPorEmailAsync(string email)
    {
        // Normalizado a minusculas: SQLite compara strings case-sensitive por defecto.
        var emailNormalizado = email.Trim().ToLower();

        return await _contexto.AsignacionesHerencia
            .Where(a => a.UsuarioId == null && a.EmailInvitado.ToLower() == emailNormalizado)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<AsignacionHerencia?> ObtenerPorTokenInvitacionAsync(string token)
    {
        return await _contexto.AsignacionesHerencia
            .Include(a => a.ActivoDigital)
            .FirstOrDefaultAsync(a => a.TokenInvitacion == token);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AsignacionHerencia>> ObtenerAceptadasPorOtorganteAsync(int usuarioOtorganteId)
    {
        return await _contexto.AsignacionesHerencia
            .Include(a => a.Usuario)
            .Where(a => a.ActivoDigital.UsuarioId == usuarioOtorganteId && a.Estado == EstadoBeneficiario.Aceptado)
            .ToListAsync();
    }
}
