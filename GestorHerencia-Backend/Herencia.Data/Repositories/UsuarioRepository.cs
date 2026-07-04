using Herencia.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Herencia.Data.Repositories;

// UsuarioRepository hereda TODO el CRUD generico de RepositorioBase<Usuario>
// (ObtenerTodosAsync, ObtenerPorIdAsync, AgregarAsync, ActualizarAsync,
// EliminarAsync) sin tener que reescribir una sola linea de esa logica, y
// ademas implementa IUsuarioRepository para sumar la consulta especifica
// ObtenerConBeneficiariosAsync. Este es el beneficio central del patron
// Repositorio: reutilizar lo comun (base) y extender solo lo particular
// (especifico) de cada entidad del dominio.
public class UsuarioRepository : RepositorioBase<Usuario>, IUsuarioRepository
{
    // El constructor simplemente reenvia el AppDbContext inyectado hacia la
    // clase base (RepositorioBase<Usuario>), que es quien realmente lo guarda
    // en el campo protegido "_contexto". No duplicamos el campo aca.
    public UsuarioRepository(AppDbContext contexto) : base(contexto)
    {
    }

    // ObtenerConBeneficiariosAsync: busca un Usuario por Id y trae de forma
    // ANSIOSA (Eager Loading) su coleccion de Beneficiarios relacionados.
    public async Task<Usuario?> ObtenerConBeneficiariosAsync(int usuarioId)
    {
        // Por que .Include() en vez de dejar que EF Core traiga solo el Usuario?
        // Por defecto, EF Core NO carga las propiedades de navegacion (relaciones)
        // de una entidad al consultarla: esto se llama "Lazy/No Loading" y evita
        // traer datos que quizas no se van a usar. El metodo .Include(u => u.Beneficiarios)
        // le indica EXPLICITAMENTE a EF Core que genere un JOIN (o una segunda
        // consulta, segun el proveedor) para traer tambien los Beneficiarios
        // asociados a este Usuario en la MISMA operacion de consulta.
        //
        // La alternativa seria "Lazy Loading" (cargar los Beneficiarios recien
        // cuando se accede a la propiedad u.Beneficiarios), pero eso puede generar
        // el problema conocido como "N+1 queries" (una consulta extra por cada
        // Usuario recorrido) y ademas requiere proxies especiales de EF Core.
        // Con Eager Loading evitamos ese problema: sabemos de antemano que
        // necesitamos los Beneficiarios, entonces los pedimos todos juntos.
        //
        // FirstOrDefaultAsync (en vez de FindAsync) es necesario aca porque FindAsync
        // NO permite combinarse con Include(): FindAsync busca directo por clave
        // primaria contra el Change Tracker/base de datos sin aceptar operadores
        // LINQ adicionales. Al usar Include() ya estamos armando una consulta LINQ
        // compuesta, por eso filtramos con Where()/FirstOrDefaultAsync().
        return await _contexto.Usuarios
            .Include(u => u.Beneficiarios)
            .FirstOrDefaultAsync(u => u.Id == usuarioId);
    }
}
