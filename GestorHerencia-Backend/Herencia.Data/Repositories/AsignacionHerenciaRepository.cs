using Herencia.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Herencia.Data.Repositories;

/// <summary>Repositorio de AsignacionHerencia: hereda el CRUD genérico y suma las consultas de <see cref="IAsignacionHerenciaRepository"/>.</summary>
public class AsignacionHerenciaRepository : RepositorioBase<AsignacionHerencia>, IAsignacionHerenciaRepository
{
    public AsignacionHerenciaRepository(AppDbContext contexto) : base(contexto)
    {
    }

    /// <summary>Ver <see cref="IAsignacionHerenciaRepository.ObtenerPorActivoDigitalAsync"/>.</summary>
    public async Task<IEnumerable<AsignacionHerencia>> ObtenerPorActivoDigitalAsync(int activoDigitalId)
    {
        return await _contexto.AsignacionesHerencia
            .Where(a => a.ActivoDigitalId == activoDigitalId)
            .ToListAsync();
    }

    /// <summary>Ver <see cref="IAsignacionHerenciaRepository.ObtenerConActivoDigitalAsync"/>.</summary>
    public async Task<AsignacionHerencia?> ObtenerConActivoDigitalAsync(int id)
    {
        return await _contexto.AsignacionesHerencia
            .Include(a => a.ActivoDigital)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    /// <summary>Ver <see cref="IAsignacionHerenciaRepository.ObtenerPorUsuarioBeneficiarioAsync"/>.</summary>
    public async Task<IEnumerable<AsignacionHerencia>> ObtenerPorUsuarioBeneficiarioAsync(int usuarioId)
    {
        return await _contexto.AsignacionesHerencia
            .Include(a => a.ActivoDigital)
            .Where(a => a.UsuarioId == usuarioId)
            .ToListAsync();
    }

    /// <summary>Ver <see cref="IAsignacionHerenciaRepository.ObtenerPendientesPorEmailAsync"/>.</summary>
    public async Task<IEnumerable<AsignacionHerencia>> ObtenerPendientesPorEmailAsync(string email)
    {
        // Se normaliza a minúsculas en ambos lados porque SQLite compara strings de forma
        // case-sensitive por defecto, y el email tipeado al invitar podría diferir en
        // mayúsculas/minúsculas del que use la otra persona al registrarse.
        var emailNormalizado = email.Trim().ToLower();

        return await _contexto.AsignacionesHerencia
            .Where(a => a.UsuarioId == null && a.EmailInvitado.ToLower() == emailNormalizado)
            .ToListAsync();
    }

    /// <summary>Ver <see cref="IAsignacionHerenciaRepository.ObtenerPorTokenInvitacionAsync"/>.</summary>
    public async Task<AsignacionHerencia?> ObtenerPorTokenInvitacionAsync(string token)
    {
        return await _contexto.AsignacionesHerencia
            .Include(a => a.ActivoDigital)
            .FirstOrDefaultAsync(a => a.TokenInvitacion == token);
    }

    /// <summary>Ver <see cref="IAsignacionHerenciaRepository.ObtenerAceptadasPorOtorganteAsync"/>.</summary>
    public async Task<IEnumerable<AsignacionHerencia>> ObtenerAceptadasPorOtorganteAsync(int usuarioOtorganteId)
    {
        return await _contexto.AsignacionesHerencia
            .Include(a => a.Usuario)
            .Where(a => a.ActivoDigital.UsuarioId == usuarioOtorganteId && a.Estado == EstadoBeneficiario.Aceptado)
            .ToListAsync();
    }
}
