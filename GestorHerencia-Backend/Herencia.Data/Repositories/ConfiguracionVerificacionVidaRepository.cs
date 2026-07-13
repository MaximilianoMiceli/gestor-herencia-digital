using Herencia.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Herencia.Data.Repositories;

/// <summary>Repositorio de ConfiguracionVerificacionVida: hereda el CRUD genérico y suma las consultas de <see cref="IConfiguracionVerificacionVidaRepository"/>.</summary>
public class ConfiguracionVerificacionVidaRepository
    : RepositorioBase<ConfiguracionVerificacionVida>, IConfiguracionVerificacionVidaRepository
{
    public ConfiguracionVerificacionVidaRepository(AppDbContext contexto) : base(contexto)
    {
    }

    /// <inheritdoc />
    public async Task<ConfiguracionVerificacionVida?> ObtenerPorUsuarioIdAsync(int usuarioId)
    {
        return await _contexto.ConfiguracionesVerificacionVida
            .Include(c => c.ContactoConfianza)
            .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ConfiguracionVerificacionVida>> ObtenerActivasParaEscaneoAsync()
    {
        return await _contexto.ConfiguracionesVerificacionVida
            .Include(c => c.Usuario)
            .Include(c => c.ContactoConfianza)
            .Where(c => c.Activo)
            .ToListAsync();
    }
}
