namespace Herencia.Business.Exceptions;

/// <summary>
/// Excepción para cuando se pide un recurso por Id y no existe (típicamente HTTP 404),
/// distinguible mediante <c>catch (RecursoNoEncontradoException)</c>.
/// </summary>
public class RecursoNoEncontradoException : Exception
{
    public RecursoNoEncontradoException(string mensaje) : base(mensaje)
    {
    }

    public RecursoNoEncontradoException(string mensaje, Exception innerException)
        : base(mensaje, innerException)
    {
    }
}
