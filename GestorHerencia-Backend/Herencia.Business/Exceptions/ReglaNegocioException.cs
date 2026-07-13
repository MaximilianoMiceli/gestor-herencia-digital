namespace Herencia.Business.Exceptions;

/// <summary>
/// Excepción "comodín" para violaciones de reglas de negocio (ej: email con formato
/// inválido) y para envolver errores técnicos inesperados (fallas de base de datos, etc.)
/// con un mensaje genérico y seguro. El detalle técnico real se conserva únicamente en
/// <see cref="Exception.InnerException"/>, nunca expuesto al cliente de la API, para no
/// filtrar información de la infraestructura (motor de base de datos, rutas, esquema).
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
