namespace Herencia.Business.Exceptions;

/// <summary>
/// Excepción para errores durante el proceso de autenticación (ej: generación de un token
/// JWT). Se distingue de <see cref="ReglaNegocioException"/> porque representa un fallo
/// del servidor (configuración/infraestructura), no datos inválidos enviados por el
/// cliente. El mensaje nunca debe incluir detalles técnicos (clave secreta, StackTrace de
/// la librería JWT) que puedan facilitar un ataque dirigido a forjar tokens.
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
