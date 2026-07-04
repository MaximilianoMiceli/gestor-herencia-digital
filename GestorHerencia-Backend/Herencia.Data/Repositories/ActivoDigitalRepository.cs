using Herencia.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Herencia.Data.Repositories;

// ActivoDigitalRepository hereda el CRUD generico de RepositorioBase<ActivoDigital>
// y suma la consulta especifica ObtenerActivosPorUsuarioAsync, siguiendo el mismo
// esquema que UsuarioRepository: reutilizar lo generico, extender solo lo particular.
public class ActivoDigitalRepository : RepositorioBase<ActivoDigital>, IActivoDigitalRepository
{
    // Reenviamos el AppDbContext inyectado hacia RepositorioBase<ActivoDigital>,
    // que es quien lo almacena en el campo protegido "_contexto".
    public ActivoDigitalRepository(AppDbContext contexto) : base(contexto)
    {
    }

    // ObtenerActivosPorUsuarioAsync: filtra los ActivosDigitales que pertenecen
    // a un Usuario especifico, usando LINQ sobre el DbSet correspondiente.
    public async Task<IEnumerable<ActivoDigital>> ObtenerActivosPorUsuarioAsync(int usuarioId)
    {
        // .Where(a => a.UsuarioId == usuarioId) es un operador LINQ que EF Core
        // TRADUCE a una clausula SQL "WHERE UsuarioId = @usuarioId": el filtrado
        // se ejecuta del lado del motor de base de datos (no se traen TODOS los
        // activos a memoria para filtrarlos despues en C#). Esto es fundamental
        // para el rendimiento: solo viaja por la red la porcion de datos que
        // realmente nos interesa.
        //
        // ToListAsync() materializa el resultado de forma asincrona: el hilo que
        // ejecuta este metodo queda libre mientras la base de datos procesa el
        // filtro y arma el resultset, en vez de bloquearse esperando esa respuesta.
        return await _contexto.ActivosDigitales
            .Where(a => a.UsuarioId == usuarioId)
            .ToListAsync();
    }
}
