namespace Herencia.Business.Exceptions;

/// <summary>
/// Excepción "comodín" para violaciones de reglas de negocio y para envolver errores
/// técnicos inesperados con un mensaje genérico y seguro. El detalle técnico real se
/// conserva únicamente en <see cref="Exception.InnerException"/>, nunca expuesto al cliente.
/// </summary>
public class ReglaNegocioException : Exception
{
    public ReglaNegocioException(string mensaje) : base(mensaje)
    {
    }

    public ReglaNegocioException(string mensaje, Exception innerException)
        : base(mensaje, innerException)
    {
    }
}
