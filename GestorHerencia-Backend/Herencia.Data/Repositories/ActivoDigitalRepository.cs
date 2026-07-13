using Herencia.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Herencia.Data.Repositories;

/// <summary>Repositorio de ActivoDigital: hereda el CRUD genérico y suma las consultas de <see cref="IActivoDigitalRepository"/>.</summary>
public class ActivoDigitalRepository : RepositorioBase<ActivoDigital>, IActivoDigitalRepository
{
    public ActivoDigitalRepository(AppDbContext contexto) : base(contexto)
    {
    }

    /// <summary>Ver <see cref="IActivoDigitalRepository.ObtenerActivosPorUsuarioAsync"/>.</summary>
    public async Task<IEnumerable<ActivoDigital>> ObtenerActivosPorUsuarioAsync(int usuarioId)
    {
        return await _contexto.ActivosDigitales
            .Where(a => a.UsuarioId == usuarioId)
            .ToListAsync();
    }

    /// <summary>Ver <see cref="IActivoDigitalRepository.ObtenerActivosPorUsuarioPaginadoAsync"/>.</summary>
    public async Task<(IEnumerable<ActivoDigital> Items, int Total)> ObtenerActivosPorUsuarioPaginadoAsync(
        int usuarioId, int pagina, int limite, TipoActivoDigital? tipo, string? nombre)
    {
        // Consulta base (IQueryable, todavía no ejecutada) reutilizada tanto para el Count
        // como para la página, evitando repetir el Where() a mano.
        var consultaBase = _contexto.ActivosDigitales.Where(a => a.UsuarioId == usuarioId);

        if (tipo is not null)
        {
            consultaBase = consultaBase.Where(a => a.Tipo == tipo);
        }

        // Se usa EF.Functions.Like en vez de string.Contains(): el proveedor de SQLite traduce
        // Contains() a instr(), que es case-sensitive, mientras que LIKE es case-insensitive
        // para ASCII, el comportamiento esperado en una búsqueda de texto para el usuario final.
        if (!string.IsNullOrWhiteSpace(nombre))
        {
            consultaBase = consultaBase.Where(a => EF.Functions.Like(a.Nombre, $"%{nombre}%"));
        }

        // Total de registros filtrados (sin paginar), para que el cliente calcule la cantidad
        // de páginas. Se ejecuta como consulta separada (COUNT) para no traer las filas.
        var total = await consultaBase.CountAsync();

        // OrderBy(a => a.Id) antes de Skip/Take es imprescindible: SQL no garantiza ningún orden
        // de filas sin un ORDER BY explícito, por lo que sin él dos ejecuciones de la misma
        // consulta paginada podrían devolver las filas en distinto orden y Skip/Take cortaría en
        // lugares inconsistentes entre una página y la siguiente (un mismo activo repetido en dos
        // páginas, o ausente en todas). Ordenar por Id (PK, siempre única) hace el resultado
        // determinístico: páginas consecutivas quedan garantizadas como conjuntos disjuntos.
        var items = await consultaBase
            .OrderBy(a => a.Id)
            .Skip((pagina - 1) * limite)
            .Take(limite)
            .ToListAsync();

        return (items, total);
    }
}
