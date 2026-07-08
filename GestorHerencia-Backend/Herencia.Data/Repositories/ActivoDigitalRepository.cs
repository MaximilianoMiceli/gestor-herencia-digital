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

    // ObtenerActivosPorUsuarioPaginadoAsync: misma idea que el metodo anterior,
    // pero trayendo solo un "recorte" (pagina) del resultado total, mediante
    // los operadores LINQ Skip() y Take().
    public async Task<(IEnumerable<ActivoDigital> Items, int Total)> ObtenerActivosPorUsuarioPaginadoAsync(
        int usuarioId, int pagina, int limite, TipoActivoDigital? tipo, string? nombre)
    {
        // Armamos la consulta BASE (el filtro por usuario) una sola vez, sin
        // ejecutarla todavia contra la base de datos: en LINQ-to-Entities, una
        // consulta es solo una "receta" (un IQueryable) hasta que se la
        // materializa con algo como ToListAsync() o CountAsync(). Reutilizar
        // esta misma receta para las dos consultas de abajo evita repetir el
        // Where() dos veces "a mano".
        var consultaBase = _contexto.ActivosDigitales.Where(a => a.UsuarioId == usuarioId);

        // --- Filtro OPCIONAL por Tipo (ej: solo "CuentaBancaria") ---
        // "tipo" es un TipoActivoDigital? (nullable): si el cliente NO mando
        // este parametro por query string, "tipo" llega null y el Where() de
        // abajo se vuelve un "no-op" (el operador "||" corta en corto en
        // "tipo == null", sin evaluar "a.Tipo == tipo" para NINGUNA fila, asi
        // que EF Core ni siquiera agrega esa condicion al SQL generado).
        if (tipo is not null)
        {
            consultaBase = consultaBase.Where(a => a.Tipo == tipo);
        }

        // --- Filtro OPCIONAL por Nombre (busqueda de texto parcial) ---
        // Se usa EF.Functions.Like con comodines "%" (no string.Contains) para
        // permitir una busqueda tipo "coincide en cualquier parte del texto".
        // OJO: string.Contains() en el proveedor de SQLite de EF Core se
        // traduce a la funcion "instr()", que en SQLite es CASE-SENSITIVE
        // (con Contains(), buscar "galicia" NO encontraria "Cuenta Banco
        // Galicia"). En cambio, el operador SQL "LIKE" es CASE-INSENSITIVE
        // por defecto en SQLite (para caracteres ASCII), que es el
        // comportamiento esperable para una busqueda de texto pensada para
        // un usuario final. Por eso se usa EF.Functions.Like en vez de
        // Contains(): ambos se traducen a SQL, pero solo LIKE ignora
        // mayusculas/minusculas aca.
        if (!string.IsNullOrWhiteSpace(nombre))
        {
            consultaBase = consultaBase.Where(a => EF.Functions.Like(a.Nombre, $"%{nombre}%"));
        }

        // Con estos dos filtros OPCIONALES sumados al UsuarioId (siempre
        // obligatorio), este metodo ya cumple "filtrar por al menos 2
        // parametros" ademas de la paginacion: un cliente puede combinar, por
        // ejemplo, "?tipo=CuentaBancaria&nombre=santander&pagina=1&limite=10"
        // en una unica consulta.

        // --- Total de registros (sin paginar) ---
        // CountAsync() le pide a la base de datos que cuente las filas que
        // matchean el filtro (traduce a un "SELECT COUNT(*) ... WHERE
        // UsuarioId = @usuarioId"), SIN traer los datos de esas filas. Este
        // numero es el que necesita el cliente de la Api para saber, por
        // ejemplo, "el usuario tiene 37 activos en total, repartidos en 4
        // paginas de 10".
        var total = await consultaBase.CountAsync();

        // --- La "matematica" de la paginacion: Skip() y Take() ---
        // Pensemos "pagina" y "limite" como coordenadas sobre una lista ya
        // ORDENADA de resultados:
        //   - "limite" es el TAMAÑO de cada pagina (cuantos registros entran
        //     en una pantalla, ej: 10).
        //   - "pagina" es el NUMERO de pagina que el cliente esta pidiendo
        //     (arranca en 1, no en 0, porque es mas natural para un usuario
        //     humano: "quiero ver la pagina 1", no "la pagina 0").
        //
        // Skip(N) le dice a la base de datos "ignora los primeros N
        // registros" (traduce a OFFSET en SQL). Para saber CUANTOS registros
        // hay que saltear, hay que saltear TODAS las paginas ANTERIORES
        // completas: si estamos pidiendo la pagina 3 con un limite de 10,
        // hay que saltear las paginas 1 y 2 completas, es decir, 2*10 = 20
        // registros. En general: Skip = (pagina - 1) * limite.
        //   - Pagina 1, limite 10 -> Skip(0)  -> arranca desde el primer registro.
        //   - Pagina 2, limite 10 -> Skip(10) -> se salta los primeros 10.
        //   - Pagina 3, limite 10 -> Skip(20) -> se salta los primeros 20.
        //
        // Take(N), aplicado DESPUES de Skip(), le dice a la base de datos
        // "de lo que queda, traeme como MAXIMO N registros" (traduce a LIMIT
        // en SQL). Es lo que efectivamente acota el tamaño de la pagina.
        //
        // --- ¿Por que hace falta OrderBy() antes de Skip()/Take()? ---
        // SQL NO garantiza ningun orden de filas si no se pide uno
        // explicitamente con ORDER BY: sin un orden fijo, dos ejecuciones de
        // la MISMA consulta de paginacion podrian devolver las filas en
        // distinto orden (segun como el motor decida recorrer los datos
        // internamente), haciendo que Skip/Take "corte" en lugares
        // inconsistentes entre una pagina y la siguiente (ej: un mismo activo
        // podria aparecer repetido en dos paginas, o no aparecer en ninguna).
        // Ordenar por "Id" (la clave primaria, siempre unica) asegura un
        // resultado DETERMINISTICO: la pagina 1 y la pagina 2 siempre van a
        // ser conjuntos disjuntos y consistentes entre llamados sucesivos.
        var items = await consultaBase
            .OrderBy(a => a.Id)
            .Skip((pagina - 1) * limite)
            .Take(limite)
            .ToListAsync();

        return (items, total);
    }
}
