namespace Herencia.Data.Models;

// RolUsuario clasifica el NIVEL DE PERMISOS de un Usuario dentro del sistema.
// Se modela como enum (no como un string libre "admin"/"Admin"/"ADMIN") por el
// mismo motivo que TipoActivoDigital: evita datos inconsistentes y EF Core lo
// persiste como INTEGER en SQLite, ademas de que el compilador impide asignar
// un valor que no sea uno de los dos definidos.
public enum RolUsuario
{
    // Rol por defecto de CUALQUIER usuario que se registra por su cuenta
    // (POST /api/auth/registro). Puede operar unicamente sobre sus PROPIOS
    // datos (ver los chequeos de ownership en UsuariosController y
    // ActivosDigitalesController).
    Usuario = 0,

    // Rol elevado, pensado para tareas administrativas del sistema (ej:
    // listar TODOS los usuarios registrados). Nadie puede auto-asignarse
    // este rol via el endpoint publico de registro: ni RegistroDTO ni
    // UsuarioCreacionDTO exponen una propiedad "Rol", por lo que
    // UsuarioService.CrearUsuarioAsync SIEMPRE crea usuarios con
    // RolUsuario.Usuario (ver esa clase). Elevar a un usuario a
    // Administrador queda, por ahora, como una operacion manual sobre la
    // base de datos (fuera del alcance de esta etapa).
    Administrador = 1
}
