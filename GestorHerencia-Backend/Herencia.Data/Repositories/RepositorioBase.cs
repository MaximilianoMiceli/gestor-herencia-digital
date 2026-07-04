using Microsoft.EntityFrameworkCore;

namespace Herencia.Data.Repositories;

// RepositorioBase<T> es la IMPLEMENTACION concreta del contrato IRepositorioBase<T>.
// Aqui es donde realmente "tocamos" Entity Framework Core: la capa Business jamas
// hace referencia a AppDbContext ni a DbSet<T> directamente, solo conoce la interfaz.
// Esto mantiene la arquitectura en 3 capas (Api -> Business -> Data) bien separada:
// si el dia de manana cambiamos SQLite por SQL Server, o incluso EF Core por Dapper,
// solo se modifica esta clase (y otras dentro de Data); el resto de la aplicacion
// no se entera del cambio porque sigue programando contra la misma interfaz.
public class RepositorioBase<T> : IRepositorioBase<T> where T : class
{
    // El AppDbContext es la puerta de entrada a la base de datos (DbSet's, tracking,
    // transacciones, etc). Lo guardamos en un campo "protected" (no private) para que
    // las clases hijas (ej: UsuarioRepository) puedan reutilizarlo directamente y
    // acceder a otros DbSet's cuando necesiten hacer Include() entre entidades
    // relacionadas (ej: Usuario -> Beneficiarios).
    protected readonly AppDbContext _contexto;

    // Inyeccion de Dependencias: el AppDbContext NO se crea con "new" dentro del
    // repositorio. Es el contenedor de DI configurado en la capa Api (Program.cs)
    // el que se encarga de construir e inyectar la instancia correcta (con su
    // cadena de conexion, ciclo de vida "Scoped", etc). Esto desacopla al
    // repositorio de los detalles de configuracion de la base de datos y facilita
    // muchisimo el testing (se puede inyectar un DbContext en memoria para pruebas).
    public RepositorioBase(AppDbContext contexto)
    {
        _contexto = contexto;
    }

    // ObtenerTodosAsync: trae todos los registros de la tabla T.
    public async Task<IEnumerable<T>> ObtenerTodosAsync()
    {
        // Set<T>() obtiene el DbSet<T> correspondiente (ej: DbSet<Usuario>) de forma
        // generica, sin tener que exponer cada DbSet especifico del AppDbContext.
        // ToListAsync() (en vez de ToList()) es el metodo asincrono provisto por EF Core:
        // libera el hilo mientras el motor de base de datos ejecuta el SELECT y
        // materializa los resultados, en lugar de bloquear el hilo esperando la
        // respuesta de forma sincronica. Esto es clave en una API web, donde cada
        // hilo bloqueado es un hilo menos disponible para atender a otros usuarios.
        return await _contexto.Set<T>().ToListAsync();
    }

    // ObtenerPorIdAsync: busca un unico registro por su clave primaria.
    public async Task<T?> ObtenerPorIdAsync(int id)
    {
        // FindAsync es el metodo de EF Core optimizado para buscar por PK:
        // primero revisa si la entidad ya esta siendo trackeada en memoria
        // (Change Tracker) antes de ir a la base de datos, evitando un viaje
        // innecesario si la entidad ya fue cargada previamente en este mismo
        // AppDbContext. Tambien es asincrono por el mismo motivo que ToListAsync:
        // evitar bloquear el hilo durante la espera de I/O.
        return await _contexto.Set<T>().FindAsync(id);
    }

    // AgregarAsync: inserta una nueva entidad.
    public async Task AgregarAsync(T entidad)
    {
        // AddAsync marca la entidad con el estado "Added" en el Change Tracker de
        // EF Core. Se usa la variante asincrona (en vez de Add) por convencion y
        // porque, en proveedores que usan generacion de valores del lado del
        // servidor (ej: secuencias), puede requerir una consulta asincrona interna.
        await _contexto.Set<T>().AddAsync(entidad);

        // SaveChangesAsync es el que efectivamente traduce los cambios pendientes
        // del Change Tracker a sentencias SQL (INSERT/UPDATE/DELETE) y las ejecuta
        // contra la base de datos dentro de una transaccion implicita. Sin este
        // llamado, "AddAsync" solo modificaria el estado en memoria y NUNCA se
        // persistiria nada en la base de datos real.
        await _contexto.SaveChangesAsync();
    }

    // ActualizarAsync: persiste cambios sobre una entidad existente.
    public async Task ActualizarAsync(T entidad)
    {
        // Update() marca TODA la entidad como "Modified" en el Change Tracker,
        // es decir, le dice a EF Core que genere un UPDATE con todas sus columnas
        // (a diferencia de rastrear cambio por cambio propiedad por propiedad).
        // Es una operacion sincrona en si misma (solo cambia el estado en memoria),
        // por eso no existe un "UpdateAsync" en EF Core: lo asincrono ocurre recien
        // al llamar a SaveChangesAsync, que es cuando se dispara el I/O real.
        _contexto.Set<T>().Update(entidad);
        await _contexto.SaveChangesAsync();
    }

    // EliminarAsync: borra un registro a partir de su Id.
    public async Task EliminarAsync(int id)
    {
        // Primero buscamos la entidad (de forma asincrona) porque EF Core necesita
        // una instancia trackeada para poder marcarla como "Deleted"; no se puede
        // borrar "a ciegas" solo con un numero de Id sin antes tener el objeto.
        var entidad = await _contexto.Set<T>().FindAsync(id);

        // Si no existe ningun registro con ese Id, no hacemos nada: no tiene
        // sentido lanzar una excepcion por intentar borrar algo que ya no esta,
        // la capa Business decidira como comunicar este caso (ej: devolver 404).
        if (entidad is not null)
        {
            _contexto.Set<T>().Remove(entidad);
            await _contexto.SaveChangesAsync();
        }
    }
}
