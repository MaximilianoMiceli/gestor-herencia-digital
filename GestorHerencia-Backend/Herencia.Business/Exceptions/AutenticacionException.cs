namespace Herencia.Business.Exceptions;

/// <summary>
/// Excepción para errores durante la autenticación (ej: generación de un JWT), que
/// representa un fallo del servidor y no datos inválidos del cliente. El mensaje nunca
/// debe incluir detalles técnicos que faciliten forjar tokens.
/// </summary>
public class AutenticacionException : Exception
{
    public AutenticacionException(string mensaje) : base(mensaje)
    {
    }

    public AutenticacionException(string mensaje, Exception innerException)
        : base(mensaje, innerException)
    {
    }
}
