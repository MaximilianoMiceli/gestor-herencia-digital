namespace Herencia.Business.Exceptions;

/// <summary>
/// Excepción para cuando se pide un recurso (usuario, activo digital, etc.) por Id y no
/// existe. Permite distinguir este caso (típicamente HTTP 404) de errores genéricos
/// mediante <c>catch (RecursoNoEncontradoException)</c>, en vez de inspeccionar mensajes.
/// </summary>
public class RecursoNoEncontradoException : Exception
{
    public RecursoNoEncontradoException(string mensaje) : base(mensaje)
    {
    }

    // La excepción técnica original se preserva solo en InnerException, para diagnóstico
    // interno, nunca expuesta al cliente de la API.
    public RecursoNoEncontradoException(string mensaje, Exception innerException)
        : base(mensaje, innerException)
    {
    }
}
