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
    // Ademas, reclama automaticamente cualquier AsignacionHerencia pendiente
    // (invitacion sin cuenta) que lo nombraba por este mismo Email, dejandola
    // vinculada a la cuenta recien creada (ver la implementacion para el
    // detalle). Devuelve el UsuarioDTO ya creado (con su Id autogenerado)
    // para que quien llama pueda, por ejemplo, redirigir al cliente a
    // "/api/usuarios/{id}".
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
    // AppDbContext, tambien sus ActivosDigitales asociados, en su rol de
    // otorgante). Si el Usuario todavia tiene HerenciasRecibidas activas (fue
    // designado como beneficiario de algo), el borrado se rechaza.
    // Puede lanzar RecursoNoEncontradoException si el Id no existe, o
    // ReglaNegocioException si ocurre un error tecnico al eliminarlo.
    Task EliminarUsuarioAsync(int id);

    // Busca un Usuario por su Email y devuelve un UsuarioAutenticacionDTO,
    // el UNICO DTO de este servicio que SI incluye PasswordHash/PasswordSalt
    // (ver el comentario de esa clase para el porque). Este metodo existe
    // exclusivamente para que AuthController pueda completar el flujo de
    // LOGIN: obtener los datos necesarios para verificar la contrasena
    // ingresada contra el hash/salt persistidos.
    // Puede lanzar RecursoNoEncontradoException si el email no corresponde a
    // ningun Usuario registrado, o ReglaNegocioException si ocurre un error
    // tecnico al consultarlo.
    Task<UsuarioAutenticacionDTO> ObtenerUsuarioParaAutenticacionAsync(string email);

    // CambiarPasswordAsync: cambia la contraseña de un Usuario YA
    // AUTENTICADO que conoce su contraseña actual (ver CambiarPasswordDTO).
    // Puede lanzar:
    //  - RecursoNoEncontradoException: si el Id no existe.
    //  - ReglaNegocioException: si "PasswordActual" no coincide con la
    //    contraseña realmente persistida, si "PasswordNueva" no cumple el
    //    largo minimo, o si ocurre un error tecnico al persistir el cambio.
    Task CambiarPasswordAsync(int usuarioId, CambiarPasswordDTO cambiarPasswordDTO);

    // SolicitarResetPasswordAsync: primer paso del flujo de "olvide mi
    // contraseña". Genera un token de reseteo de un solo uso y vida corta,
    // lo persiste junto a su fecha de expiracion, y lo devuelve en texto
    // plano para que el CONTROLLER lo "envie" (simulado, por consola) al
    // Email del usuario.
    //
    // Devuelve null (en vez de lanzar RecursoNoEncontradoException) si el
    // email no corresponde a ningun Usuario registrado: esto es
    // DELIBERADO, el mismo criterio anti "user enumeration" que ya aplica
    // UsuarioService.ObtenerUsuarioParaAutenticacionAsync en el Login. El
    // controller debe responder el MISMO mensaje generico de exito exista o
    // no exista esa cuenta, para no revelar por la respuesta HTTP que
    // emails estan registrados en el sistema.
    Task<string?> SolicitarResetPasswordAsync(string email);

    // ResetearPasswordAsync: segundo y ultimo paso del flujo de "olvide mi
    // contraseña". Busca al Usuario por su PasswordResetToken vigente y,
    // si todavia no expiro, reemplaza su contraseña por la nueva.
    // Puede lanzar ReglaNegocioException si el token no existe, ya expiro, o
    // "PasswordNueva" no cumple el largo minimo.
    Task ResetearPasswordAsync(ResetearPasswordDTO resetearPasswordDTO);
}
