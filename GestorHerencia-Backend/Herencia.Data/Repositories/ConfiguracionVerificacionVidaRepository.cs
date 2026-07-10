using Herencia.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Herencia.Data.Repositories;

// ConfiguracionVerificacionVidaRepository hereda el CRUD generico de
// RepositorioBase<ConfiguracionVerificacionVida> (AgregarAsync/ActualizarAsync
// ya alcanzan para crear/editar la configuracion de un titular, dado que la
// PK es compartida con Usuario) y suma las dos consultas especificas de este
// dominio.
public class ConfiguracionVerificacionVidaRepository
    : RepositorioBase<ConfiguracionVerificacionVida>, IConfiguracionVerificacionVidaRepository
{
    public ConfiguracionVerificacionVidaRepository(AppDbContext contexto) : base(contexto)
    {
    }

    public async Task<ConfiguracionVerificacionVida?> ObtenerPorUsuarioIdAsync(int usuarioId)
    {
        // FindAsync (heredado via ObtenerPorIdAsync) serviria igual, ya que
        // UsuarioId ES la PK de esta tabla, pero se usa un metodo con
        // nombre propio para no obligar a quien lea Business a recordar
        // ese detalle de modelado. Se incluye ContactoConfianza (Include)
        // para que el servicio pueda armar ConfiguracionVerificacionVidaDTO.ContactoConfianzaNombre
        // sin una consulta adicional aparte.
        return await _contexto.ConfiguracionesVerificacionVida
            .Include(c => c.ContactoConfianza)
            .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);
    }

    public async Task<IEnumerable<ConfiguracionVerificacionVida>> ObtenerActivasParaEscaneoAsync()
    {
        // Include de Usuario (el titular) y ContactoConfianza: el job de
        // escaneo (VerificacionVidaService.EjecutarEscaneoAsync) necesita
        // Nombre/Email de ambos para armar las notificaciones, sin una
        // consulta adicional por cada fila.
        return await _contexto.ConfiguracionesVerificacionVida
            .Include(c => c.Usuario)
            .Include(c => c.ContactoConfianza)
            .Where(c => c.Activo)
            .ToListAsync();
    }
}
