namespace Herencia.Business.Exceptions;

// AutenticacionException es la excepcion PERSONALIZADA para todo lo que sale mal
// durante el proceso de AUTENTICACION: puntualmente, la generacion de un Token
// JWT (ver TokenService.CrearToken). Se crea una clase separada de
// ReglaNegocioException (aunque conceptualmente podria pensarse como "un caso
// mas" de regla de negocio) porque semanticamente representa un dominio de
// error bien distinto: no es que el USUARIO haya mandado datos invalidos, es
// que el SERVIDOR no pudo completar el proceso de login/generacion de
// credenciales (ej: falta la clave secreta en la configuracion, o la libreria
// de JWT fallo al firmar el token). Tener una excepcion propia permite, en una
// futura capa Api, atrapar "catch (AutenticacionException ex)" y devolver
// siempre un codigo HTTP coherente (tipicamente 500, porque el problema es de
// configuracion/infraestructura del servidor, no del cliente).
//
// SEGURIDAD: el mensaje que viaja en esta excepcion NUNCA debe incluir detalles
// tecnicos del error real (ej: el valor de la clave secreta, el stack trace de
// la libreria JWT, rutas de archivos de configuracion). Revelar esos detalles a
// un atacante podria facilitar un ataque dirigido a debilitar/forjar tokens.
public class AutenticacionException : Exception
{
    // Constructor simple: mensaje amigable y seguro, sin excepcion tecnica
    // subyacente que preservar (ej: "no se encontro la clave de configuracion").
    public AutenticacionException(string mensaje) : base(mensaje)
    {
    }

    // Constructor con inner exception: preserva el error tecnico ORIGINAL
    // (ej: una excepcion de la libreria System.IdentityModel.Tokens.Jwt) unicamente
    // en la propiedad heredada "InnerException", disponible solo para logging
    // interno del servidor, nunca para el cliente de la Api.
    public AutenticacionException(string mensaje, Exception innerException)
        : base(mensaje, innerException)
    {
    }
}
