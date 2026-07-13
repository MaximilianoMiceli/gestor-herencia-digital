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

    /// <summary>Ver <see cref="IConfiguracionVerificacionVidaRepository.ObtenerPorUsuarioIdAsync"/>.</summary>
    public async Task<ConfiguracionVerificacionVida?> ObtenerPorUsuarioIdAsync(int usuarioId)
    {
        // Se incluye ContactoConfianza para que el servicio arme
        // ConfiguracionVerificacionVidaDTO.ContactoConfianzaNombre sin una consulta adicional.
        return await _contexto.ConfiguracionesVerificacionVida
            .Include(c => c.ContactoConfianza)
            .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);
    }

    /// <summary>Ver <see cref="IConfiguracionVerificacionVidaRepository.ObtenerActivasParaEscaneoAsync"/>.</summary>
    public async Task<IEnumerable<ConfiguracionVerificacionVida>> ObtenerActivasParaEscaneoAsync()
    {
        // Include de Usuario (titular) y ContactoConfianza: el job de escaneo necesita
        // Nombre/Email de ambos para armar notificaciones, sin consultas por cada fila.
        return await _contexto.ConfiguracionesVerificacionVida
            .Include(c => c.Usuario)
            .Include(c => c.ContactoConfianza)
            .Where(c => c.Activo)
            .ToListAsync();
    }
}
