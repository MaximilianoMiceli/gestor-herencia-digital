using Herencia.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Herencia.Data.Repositories;

// UsuarioRepository hereda TODO el CRUD generico de RepositorioBase<Usuario>
// (ObtenerTodosAsync, ObtenerPorIdAsync, AgregarAsync, ActualizarAsync,
// EliminarAsync) sin tener que reescribir una sola linea de esa logica, y
// ademas implementa IUsuarioRepository para sumar la consulta especifica
// ObtenerPorEmailAsync. Este es el beneficio central del patron Repositorio:
// reutilizar lo comun (base) y extender solo lo particular (especifico) de
// cada entidad del dominio.
public class UsuarioRepository : RepositorioBase<Usuario>, IUsuarioRepository
{
    // El constructor simplemente reenvia el AppDbContext inyectado hacia la
    // clase base (RepositorioBase<Usuario>), que es quien realmente lo guarda
    // en el campo protegido "_contexto". No duplicamos el campo aca.
    public UsuarioRepository(AppDbContext contexto) : base(contexto)
    {
    }

    // ObtenerPorEmailAsync: busca un Usuario filtrando por su columna Email.
    public async Task<Usuario?> ObtenerPorEmailAsync(string email)
    {
        // A diferencia de ObtenerPorIdAsync (que usa FindAsync contra la clave
        // primaria), ObtenerPorEmailAsync filtra por una columna que NO es la
        // PK, por lo que necesitamos una consulta LINQ explicita con Where()
        // (traducida por EF Core a un "WHERE Email = @email" en SQL) en vez de
        // FindAsync. Gracias al indice UNICO sobre Email definido en
        // AppDbContext.OnModelCreating, esta busqueda es eficiente y, ademas,
        // la base de datos GARANTIZA que como mucho hay un Usuario con ese
        // email exacto (por eso FirstOrDefaultAsync es semanticamente correcto
        // aca, y no traeria resultados ambiguos).
        return await _contexto.Usuarios
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    // ObtenerPorPasswordResetTokenAsync: busca un Usuario filtrando por su
    // columna PasswordResetToken.
    public async Task<Usuario?> ObtenerPorPasswordResetTokenAsync(string token)
    {
        return await _contexto.Usuarios
            .FirstOrDefaultAsync(u => u.PasswordResetToken == token);
    }

    // ObtenerPorDniAsync: busca un Usuario filtrando por su columna Dni.
    public async Task<Usuario?> ObtenerPorDniAsync(string dni)
    {
        return await _contexto.Usuarios
            .FirstOrDefaultAsync(u => u.Dni == dni);
    }
}
