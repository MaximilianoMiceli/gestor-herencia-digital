using Herencia.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Herencia.Data.Repositories;

// AsignacionHerenciaRepository hereda el CRUD generico (incluida la nueva
// EjecutarEnTransaccionAsync) de RepositorioBase<AsignacionHerencia> y suma
// las consultas especificas de esta entidad de asociacion N-N.
public class AsignacionHerenciaRepository : RepositorioBase<AsignacionHerencia>, IAsignacionHerenciaRepository
{
    public AsignacionHerenciaRepository(AppDbContext contexto) : base(contexto)
    {
    }

    // ObtenerPorActivoDigitalAsync: todas las asignaciones de un ActivoDigital.
    public async Task<IEnumerable<AsignacionHerencia>> ObtenerPorActivoDigitalAsync(int activoDigitalId)
    {
        return await _contexto.AsignacionesHerencia
            .Where(a => a.ActivoDigitalId == activoDigitalId)
            .ToListAsync();
    }

    // ObtenerConActivoDigitalAsync: busca una asignacion por Id, con su
    // ActivoDigital ya cargado (Include), para poder leer
    // asignacion.ActivoDigital.UsuarioId sin una consulta adicional aparte.
    public async Task<AsignacionHerencia?> ObtenerConActivoDigitalAsync(int id)
    {
        return await _contexto.AsignacionesHerencia
            .Include(a => a.ActivoDigital)
            .FirstOrDefaultAsync(a => a.Id == id);
    }
}
