namespace Herencia.Business.Exceptions;

// RecursoNoEncontradoException es una excepcion PERSONALIZADA (hereda de la clase
// base "Exception" de .NET) que representa un unico escenario de negocio muy
// puntual: el llamador pidio un recurso (un Usuario, un ActivoDigital, etc.) por
// su Id y ese recurso NO existe en la base de datos.
//
// Por que crear una clase propia en vez de lanzar "throw new Exception(...)"
// directamente?
// 1) Semantica: al atrapar excepciones mas arriba (ej: en un controller de la
//    capa Api en una etapa futura), podemos usar "catch (RecursoNoEncontradoException ex)"
//    para distinguir ESTE caso puntual (que normalmente se traduce a un HTTP 404
//    Not Found) de cualquier otro error generico (que se traduciria a un 500).
//    Si siempre lanzaramos "Exception" pelada, todos los catch se verian obligados
//    a inspeccionar el mensaje de texto para saber que paso, lo cual es fragil
//    y propenso a errores.
// 2) Encapsulamiento del mensaje: el mensaje que viaja dentro de esta excepcion
//    esta pensado para ser "amigable" (legible por un usuario final o un cliente
//    de la API), nunca un detalle tecnico de infraestructura.
public class RecursoNoEncontradoException : Exception
{
    // Constructor simple: recibe unicamente el mensaje amigable a mostrar
    // (ej: "No se encontro el usuario con Id 5"). Se usa cuando el propio
    // servicio de Business detecta la ausencia del recurso (por ejemplo,
    // el repositorio devolvio null) y no hay ninguna excepcion tecnica
    // subyacente que preservar.
    public RecursoNoEncontradoException(string mensaje) : base(mensaje)
    {
    }

    // Constructor con "inner exception": ademas del mensaje amigable, recibe la
    // excepcion tecnica ORIGINAL que disparo este error (ej: una excepcion de
    // EF Core o del motor de base de datos). Esa excepcion original NO se expone
    // al llamador (no se imprime su StackTrace ni su mensaje tecnico), pero se
    // guarda internamente en la propiedad heredada "InnerException" para que,
    // si en el futuro se agrega un sistema de logging, se pueda registrar el
    // detalle tecnico completo con fines de diagnostico SIN filtrar esa
    // informacion sensible hacia el cliente final de la API.
    public RecursoNoEncontradoException(string mensaje, Exception innerException)
        : base(mensaje, innerException)
    {
    }
}
