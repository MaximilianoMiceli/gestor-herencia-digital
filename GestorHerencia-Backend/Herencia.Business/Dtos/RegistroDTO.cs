namespace Herencia.Business.Dtos;

// RegistroDTO es el "contrato" de entrada para el auto-registro publico de un
// nuevo Usuario (POST /api/auth/registro). Hereda directamente de
// UsuarioCreacionDTO (mismos campos: Nombre, Email, Password, con las mismas
// validaciones [Required]) en vez de duplicar esas tres propiedades en una
// clase nueva: hoy el "alta administrativa" de un Usuario (POST /api/usuarios)
// y el "auto-registro" de un visitante (POST /api/auth/registro) necesitan
// exactamente los mismos datos de entrada.
//
// Se mantiene como una clase PROPIA (y no se reutiliza UsuarioCreacionDTO tal
// cual en el endpoint de registro) porque, conceptualmente, son dos casos de
// uso distintos que podrian DIVERGIR en el futuro sin avisar (ej: el registro
// publico podria en algun momento requerir aceptar Terminos y Condiciones, o
// nunca permitir asignar un Rol de administrador, mientras que el alta
// administrativa si podria hacerlo). Tener el nombre "RegistroDTO" documenta
// esa intencion desde el flujo de autenticacion, sin repetir campos hoy.
public class RegistroDTO : UsuarioCreacionDTO
{
}
