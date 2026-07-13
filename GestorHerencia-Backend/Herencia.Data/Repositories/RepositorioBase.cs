using Microsoft.EntityFrameworkCore;

namespace Herencia.Data.Repositories;

/// <summary>
/// Implementación concreta de <see cref="IRepositorioBase{T}"/> sobre EF Core.
/// Es la única clase que conoce <see cref="AppDbContext"/> directamente; el resto de la
/// aplicación depende solo de la interfaz, lo que mantiene aislada la capa Data.
/// </summary>
public class RepositorioBase<T> : IRepositorioBase<T> where T : class
{
    // Campo protected (no private) para que las clases hijas puedan reutilizarlo
    // y acceder a otros DbSet's cuando necesiten Include() entre entidades relacionadas.
    protected readonly AppDbContext _contexto;

    /// <summary>Recibe el <see cref="AppDbContext"/> vía inyección de dependencias (ciclo de vida Scoped).</summary>
    public RepositorioBase(AppDbContext contexto)
    {
        _contexto = contexto;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<T>> ObtenerTodosAsync()
    {
        return await _contexto.Set<T>().ToListAsync();
    }

    /// <inheritdoc />
    public async Task<T?> ObtenerPorIdAsync(int id)
    {
        // FindAsync revisa primero el Change Tracker antes de ir a la base de datos.
        return await _contexto.Set<T>().FindAsync(id);
    }

    /// <inheritdoc />
    public async Task AgregarAsync(T entidad)
    {
        await _contexto.Set<T>().AddAsync(entidad);
        await _contexto.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task ActualizarAsync(T entidad)
    {
        // Update() marca toda la entidad como "Modified" (UPDATE con todas sus columnas).
        _contexto.Set<T>().Update(entidad);
        await _contexto.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task EliminarAsync(int id)
    {
        var entidad = await _contexto.Set<T>().FindAsync(id);

        // Borrar un Id inexistente no es un error: la capa Business decide cómo comunicarlo (ej: 404).
        if (entidad is not null)
        {
            _contexto.Set<T>().Remove(entidad);
            await _contexto.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task EjecutarEnTransaccionAsync(Func<Task> operacion)
    {
        // Transacción real a nivel de conexión: mientras está abierta, cualquier SaveChangesAsync()
        // sobre este mismo AppDbContext (desde este repositorio u otro que comparta la instancia
        // Scoped dentro de la misma request) queda encolado en ella en vez de confirmarse solo.
        await using var transaccion = await _contexto.Database.BeginTransactionAsync();

        try
        {
            await operacion();
            await transaccion.CommitAsync();
        }
        catch
        {
            // Revierte todo lo intentado dentro de "operacion", sin importar en qué paso falló.
            await transaccion.RollbackAsync();
            throw;
        }
    }
}
