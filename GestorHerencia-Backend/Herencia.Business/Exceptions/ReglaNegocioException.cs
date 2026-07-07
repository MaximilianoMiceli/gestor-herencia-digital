namespace Herencia.Business.Exceptions;

// ReglaNegocioException es la excepcion PERSONALIZADA "comodin" para todo lo que
// NO es un simple "recurso no encontrado" (para eso ya existe RecursoNoEncontradoException).
// Se usa en dos escenarios bien distintos, pero relacionados:
//
// 1) VALIDACIONES DE NEGOCIO que fallan ANTES de tocar la base de datos
//    (ej: el email tiene un formato invalido, el nombre viene vacio, la
//    contrasena es demasiado corta). En este caso, el mensaje que viaja en la
//    excepcion es literalmente el motivo de la validacion, pensado para
//    mostrarse tal cual al usuario final (ej: "El email ingresado no tiene un
//    formato valido.").
//
// 2) ERRORES TECNICOS inesperados (ej: la base de datos esta caida, EF Core
//    lanzo una excepcion de conexion, un timeout, etc.) que los servicios
//    ATRAPAN dentro de un bloque try-catch y "traducen" a esta excepcion con
//    un mensaje generico y amigable (ej: "Ocurrio un error al procesar el
//    activo digital."). Esto es clave para la SEGURIDAD de la aplicacion:
//    nunca queremos que el StackTrace real, el mensaje de ADO.NET/SQLite, o
//    fragmentos de una sentencia SQL lleguen hasta el cliente de la API, ya
//    que esa informacion podria ser aprovechada por un atacante (revela
//    estructura interna de tablas, motor de base de datos usado, rutas de
//    archivos del servidor, etc.). En vez de eso, el detalle tecnico original
//    queda guardado unicamente en "InnerException", disponible solo del lado
//    del servidor para quien tenga acceso a los logs.
public class ReglaNegocioException : Exception
{
    // Constructor simple: se usa para violaciones de reglas de negocio "puras",
    // detectadas por el propio codigo de Business (no por una excepcion tecnica
    // capturada). Ejemplo: "El nombre del usuario no puede estar vacio."
    public ReglaNegocioException(string mensaje) : base(mensaje)
    {
    }

    // Constructor con "inner exception": se usa dentro de los bloques catch que
    // envuelven llamadas a los repositorios. La excepcion tecnica original
    // ("innerException", ej: una DbUpdateException de EF Core) se preserva
    // internamente para diagnostico, mientras que "mensaje" es el texto
    // generico y seguro que efectivamente ve el usuario final.
    public ReglaNegocioException(string mensaje, Exception innerException)
        : base(mensaje, innerException)
    {
    }
}
