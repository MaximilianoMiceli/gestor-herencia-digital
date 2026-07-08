namespace Herencia.Data.Repositories;

// IRepositorioBase<T> es el CONTRATO generico que define las operaciones CRUD
// (Crear, Leer, Actualizar, Borrar) que TODA entidad de nuestro dominio va a
// necesitar tarde o temprano (Usuario, Beneficiario, ActivoDigital, etc).
//
// Por que una interfaz generica y no una interfaz por entidad?
// Porque el patron Repositorio busca ELIMINAR DUPLICACION: en vez de escribir
// "ObtenerTodos", "Agregar", "Actualizar" y "Eliminar" una y otra vez para cada
// tabla, los definimos UNA sola vez aqui parametrizados por "T", y cada
// repositorio especifico (UsuarioRepository, ActivoDigitalRepository, etc.)
// hereda gratis esta funcionalidad basica.
//
// Ademas, al depender la capa Business de esta INTERFAZ (y no de la clase
// concreta ni de EF Core directamente), logramos el objetivo central de la
// Etapa 2: aislar el acceso a datos. La capa Business no sabe (ni le importa)
// si por detras hay SQLite, SQL Server o una API externa: solo conoce este
// contrato. Esto es el principio de Inversion de Dependencias (la "D" de SOLID).
//
// "where T : class" restringe el generico a tipos referencia, que es lo que
// exige EF Core para trabajar con DbSet<T>.
public interface IRepositorioBase<T> where T : class
{
    // Devuelve TODOS los registros de la tabla asociada a T.
    // Es asincrono (Task<IEnumerable<T>>) porque leer de la base de datos implica
    // una operacion de Entrada/Salida (I/O): el hilo que ejecuta este metodo NO
    // deberia quedar bloqueado esperando la respuesta del motor de base de datos;
    // en cambio, se libera para atender otras solicitudes mientras la consulta
    // viaja a la base de datos y vuelve. Esto mejora enormemente la escalabilidad
    // de una API web bajo carga concurrente.
    Task<IEnumerable<T>> ObtenerTodosAsync();

    // Busca un unico registro por su clave primaria (Id).
    // Devuelve "T?" (nullable) porque es perfectamente valido que no exista
    // ningun registro con ese Id: el llamador debe estar preparado para recibir null.
    Task<T?> ObtenerPorIdAsync(int id);

    // Agrega una nueva entidad a la base de datos.
    // Devuelve Task (sin resultado) porque la operacion de escritura no necesita
    // retornar un valor: el objeto "entidad" ya viene con los datos que el
    // llamador quiere persistir, y EF Core le asignara el Id autogenerado
    // directamente sobre esa misma instancia luego del INSERT.
    Task AgregarAsync(T entidad);

    // Marca una entidad existente como modificada y persiste los cambios.
    // Se recibe la entidad completa (ya con los nuevos valores) porque en el
    // patron Repositorio delegamos en EF Core la tarea de detectar que columnas
    // cambiaron (Change Tracking) y armar el UPDATE correspondiente.
    Task ActualizarAsync(T entidad);

    // Elimina un registro a partir de su Id.
    // Se recibe solo el Id (y no la entidad completa) porque, desde la capa
    // Business, muchas veces solo se conoce el identificador (ej: viene de la
    // URL de un endpoint REST tipo DELETE /api/usuarios/5) y no hace falta
    // cargar el objeto completo para poder borrarlo.
    Task EliminarAsync(int id);

    // Ejecuta la funcion "operacion" dentro de una TRANSACCION EXPLICITA de
    // base de datos. Se agrega aca (en el repositorio GENERICO, no en uno
    // especifico) porque cualquier entidad del dominio podria, en algun
    // momento, necesitar una operacion que combine VARIOS pasos de escritura
    // (ej: varias llamadas a AgregarAsync) que deben tener exito TODAS juntas
    // o NINGUNA: por ejemplo, repartir un ActivoDigital entre multiples
    // Beneficiarios (varias filas de AsignacionHerencia) en una sola
    // operacion atomica.
    //
    // - Si "operacion" termina SIN lanzar ninguna excepcion, la transaccion
    //   se CONFIRMA (Commit): todos los cambios intentados dentro de
    //   "operacion" quedan persistidos de forma definitiva.
    // - Si "operacion" lanza CUALQUIER excepcion en CUALQUIER punto (incluso
    //   despues de haber persistido con exito ALGUNOS de los pasos
    //   anteriores dentro de la misma operacion), la transaccion se REVIERTE
    //   (Rollback) POR COMPLETO: es como si absolutamente nada de lo
    //   intentado dentro de "operacion" hubiera ocurrido, aunque parte de
    //   ese trabajo ya se hubiera "guardado" (SaveChangesAsync) en un paso
    //   intermedio. La excepcion original se relanza hacia quien llamo, para
    //   que la capa Business decida como comunicar el error (ver
    //   RepositorioBase.EjecutarEnTransaccionAsync para el detalle de
    //   implementacion).
    Task EjecutarEnTransaccionAsync(Func<Task> operacion);
}
