namespace Herencia.Data.Models;

/// <summary>Nivel de permisos de un Usuario dentro del sistema.</summary>
public enum RolUsuario
{
    // Ni RegistroDTO ni UsuarioCreacionDTO exponen "Rol": nadie puede auto-asignarse
    // Administrador via el registro publico.
    Usuario = 0,

    // Elevar a un usuario a Administrador es, por ahora, una operacion manual sobre la base de datos.
    Administrador = 1
}
