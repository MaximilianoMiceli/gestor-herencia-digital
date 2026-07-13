using Herencia.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Herencia.Data.Repositories;

/// <summary>Repositorio de ActivoDigital: hereda el CRUD genérico y suma las consultas de <see cref="IActivoDigitalRepository"/>.</summary>
public class ActivoDigitalRepository : RepositorioBase<ActivoDigital>, IActivoDigitalRepository
{
    public ActivoDigitalRepository(AppDbContext contexto) : base(contexto)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ActivoDigital>> ObtenerActivosPorUsuarioAsync(int usuarioId)
    {
        return await _contexto.ActivosDigitales
            .Where(a => a.UsuarioId == usuarioId)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<ActivoDigital> Items, int Total)> ObtenerActivosPorUsuarioPaginadoAsync(
        int usuarioId, int pagina, int limite, TipoActivoDigital? tipo, string? nombre)
    {
        var consultaBase = _contexto.ActivosDigitales.Where(a => a.UsuarioId == usuarioId);

        if (tipo is not null)
        {
            consultaBase = consultaBase.Where(a => a.Tipo == tipo);
        }

        // EF.Functions.Like (no Contains): SQLite traduce Contains() a instr(), case-sensitive,
        // mientras que LIKE es case-insensitive, el comportamiento esperado por el usuario final.
        if (!string.IsNullOrWhiteSpace(nombre))
        {
            consultaBase = consultaBase.Where(a => EF.Functions.Like(a.Nombre, $"%{nombre}%"));
        }

        var total = await consultaBase.CountAsync();

        // OrderBy(Id) antes de Skip/Take: sin un orden explicito, SQL no garantiza el orden de
        // filas y dos ejecuciones de la misma pagina podrian devolver resultados inconsistentes.
        var items = await consultaBase
            .OrderBy(a => a.Id)
            .Skip((pagina - 1) * limite)
            .Take(limite)
            .ToListAsync();

        return (items, total);
    }
}
