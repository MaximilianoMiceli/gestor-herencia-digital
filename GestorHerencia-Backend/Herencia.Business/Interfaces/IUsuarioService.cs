using Herencia.Business.Dtos;

namespace Herencia.Business.Interfaces;

// IUsuarioService es el CONTRATO publico de la logica de negocio relacionada a
// Usuario. Es el equivalente, en la capa Business, a lo que IUsuarioRepository
// es en la capa Data: asi como la capa Business jamas depende de la clase
// concreta "RepositorioBase" ni de EF Core, la futura capa Api (los controllers)
// jamas van a depender de la clase concreta "UsuarioService", sino de ESTA
// interfaz. Esto permite, entre otras cosas, registrar la implementacion en el
// contenedor de Inyeccion de Dependencias (Program.cs) e inyectarla en
// cualquier controller sin acoplarlo a los detalles internos del servicio.
//
// Notar que todos los metodos trabajan exclusivamente con DTOs (UsuarioCreacionDTO
// de entrada, UsuarioDTO de salida) y NUNCA con la entidad "Usuario" de
// Herencia.Data.Models: esa entidad es un detalle de implementacion interno de
// la capa Data/Business, y no debe "fugarse" hacia capas superiores.
public interface IUsuarioService
{
    // Da de alta un nuevo Usuario a partir de los datos basicos de registro.
    // Devuelve el UsuarioDTO ya creado (con su Id autogenerado) para que quien
    // llama pueda, por ejemplo, redirigir al cliente a "/api/usuarios/{id}".
    // Puede lanzar ReglaNegocioException (datos invalidos o error tecnico).
    Task<UsuarioDTO> CrearUsuarioAsync(UsuarioCreacionDTO usuarioCreacionDTO);

    // Busca un unico Usuario por su Id.
    // Puede lanzar RecursoNoEncontradoException si el Id no existe, o
    // ReglaNegocioException si ocurre un error tecnico al consultarlo.
    Task<UsuarioDTO> ObtenerUsuarioPorIdAsync(int id);

    // Devuelve el listado completo de Usuarios registrados.
    // Puede lanzar ReglaNegocioException si ocurre un error tecnico al consultarlos.
    Task<IEnumerable<UsuarioDTO>> ObtenerTodosLosUsuariosAsync();

    // Actualiza el Nombre y el Email de un Usuario ya existente.
    // Puede lanzar RecursoNoEncontradoException si el Id no existe, o
    // ReglaNegocioException si los nuevos datos son invalidos o si ocurre un
    // error tecnico al persistir el cambio.
    Task<UsuarioDTO> ActualizarUsuarioAsync(int id, UsuarioActualizacionDTO usuarioActualizacionDTO);

    // Elimina un Usuario existente (y, por la configuracion de cascada del
    // AppDbContext, tambien sus Beneficiarios y ActivosDigitales asociados).
    // Puede lanzar RecursoNoEncontradoException si el Id no existe, o
    // ReglaNegocioException si ocurre un error tecnico al eliminarlo.
    Task EliminarUsuarioAsync(int id);
}
