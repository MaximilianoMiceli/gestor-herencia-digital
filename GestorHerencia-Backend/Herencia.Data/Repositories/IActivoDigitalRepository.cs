using Herencia.Data.Models;

namespace Herencia.Data.Repositories;

// IActivoDigitalRepository extiende el contrato generico IRepositorioBase<ActivoDigital>
// para sumar consultas propias del dominio de ActivoDigital.
public interface IActivoDigitalRepository : IRepositorioBase<ActivoDigital>
{
    // Devuelve todos los ActivosDigitales que pertenecen a un Usuario puntual.
    // Se usa IEnumerable<ActivoDigital> (una coleccion, no un unico objeto) porque
    // un Usuario puede tener CERO, uno o muchos activos digitales registrados.
    Task<IEnumerable<ActivoDigital>> ObtenerActivosPorUsuarioAsync(int usuarioId);

    // Version PAGINADA y FILTRADA de la consulta anterior: en vez de traer
    // TODOS los activos de un usuario de una sola vez, trae solo la "pagina"
    // pedida (un subconjunto acotado), permite filtrar opcionalmente por
    // "tipo" y/o por una busqueda parcial de "nombre" (ambos nullable: si no
    // se envian, no restringen la busqueda), y ademas informa cuantos
    // registros existen EN TOTAL para esos filtros (sin paginar), dato
    // indispensable para que quien consuma la Api pueda calcular cuantas
    // paginas hay en total y mostrar un paginador. Se devuelve una tupla
    // (Items, Total) en vez de dos metodos separados para evitar tener que
    // consultar la base de datos dos veces por separado desde el llamador.
    Task<(IEnumerable<ActivoDigital> Items, int Total)> ObtenerActivosPorUsuarioPaginadoAsync(
        int usuarioId, int pagina, int limite, TipoActivoDigital? tipo, string? nombre);
}
