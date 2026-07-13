namespace Herencia.Data.Repositories;

/// <summary>
/// Contrato genérico de operaciones CRUD compartido por todas las entidades del dominio.
/// Es genérico para evitar redefinir Obtener/Agregar/Actualizar/Eliminar en cada repositorio,
/// y al ser el tipo del que depende la capa Business (en vez de EF Core o la clase concreta),
/// aísla el acceso a datos según el principio de Inversión de Dependencias.
/// </summary>
/// <typeparam name="T">Entidad del dominio (restringida a "class" porque EF Core lo exige para DbSet&lt;T&gt;).</typeparam>
public interface IRepositorioBase<T> where T : class
{
    /// <summary>Devuelve todos los registros de la tabla asociada a T.</summary>
    Task<IEnumerable<T>> ObtenerTodosAsync();

    /// <summary>Busca un registro por su Id. Devuelve null si no existe.</summary>
    Task<T?> ObtenerPorIdAsync(int id);

    /// <summary>Inserta una nueva entidad.</summary>
    Task AgregarAsync(T entidad);

    /// <summary>Persiste los cambios de una entidad ya existente.</summary>
    Task ActualizarAsync(T entidad);

    /// <summary>Elimina un registro por su Id (no-op si no existe).</summary>
    Task EliminarAsync(int id);

    /// <summary>
    /// Ejecuta <paramref name="operacion"/> dentro de una transacción explícita: si termina sin
    /// excepciones se confirma (Commit); si lanza cualquier excepción, se revierte por completo
    /// (Rollback), aunque parte del trabajo ya se haya guardado en un paso intermedio. Se define
    /// en el repositorio genérico porque cualquier entidad puede necesitar combinar varios pasos
    /// de escritura que deben tener éxito todos juntos o ninguno.
    /// </summary>
    Task EjecutarEnTransaccionAsync(Func<Task> operacion);
}
