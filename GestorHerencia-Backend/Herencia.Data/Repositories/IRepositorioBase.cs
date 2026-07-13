namespace Herencia.Data.Repositories;

/// <summary>Contrato genérico de operaciones CRUD compartido por todas las entidades del dominio.</summary>
/// <typeparam name="T">Entidad del dominio (restringida a "class" porque EF Core lo exige para DbSet&lt;T&gt;).</typeparam>
public interface IRepositorioBase<T> where T : class
{
    Task<IEnumerable<T>> ObtenerTodosAsync();

    Task<T?> ObtenerPorIdAsync(int id);

    Task AgregarAsync(T entidad);

    Task ActualizarAsync(T entidad);

    Task EliminarAsync(int id);

    /// <summary>
    /// Ejecuta <paramref name="operacion"/> en una transacción: si termina sin excepciones se
    /// confirma; si lanza cualquiera, se revierte todo lo hecho aunque haya pasos ya guardados.
    /// </summary>
    Task EjecutarEnTransaccionAsync(Func<Task> operacion);
}
