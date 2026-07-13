namespace Herencia.Data.Models;

/// <summary>Nivel de permisos de un Usuario dentro del sistema.</summary>
// Enum (no string libre) por el mismo motivo que TipoActivoDigital: evita datos
// inconsistentes y se persiste como INTEGER en SQLite.
public enum RolUsuario
{
    // Ni RegistroDTO ni UsuarioCreacionDTO exponen una propiedad "Rol", asi que
    // UsuarioService.CrearUsuarioAsync siempre crea usuarios con este rol: nadie
    // puede auto-asignarse Administrador via el registro publico.

    /// <summary>Rol por defecto: solo puede operar sobre sus propios datos.</summary>
    Usuario = 0,

    // Elevar a un usuario a Administrador es, por ahora, una operacion manual
    // sobre la base de datos (fuera del alcance de esta etapa).

    /// <summary>Rol elevado para tareas administrativas (ej: listar todos los usuarios).</summary>
    Administrador = 1
}
