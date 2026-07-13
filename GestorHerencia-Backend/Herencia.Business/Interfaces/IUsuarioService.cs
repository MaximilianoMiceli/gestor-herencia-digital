using Herencia.Business.Dtos;

namespace Herencia.Business.Interfaces;

/// <summary>
/// Contrato de la logica de negocio de Usuario. Todos los metodos trabajan
/// exclusivamente con DTOs (nunca con la entidad Usuario de Herencia.Data.Models), para
/// que la capa Api dependa unicamente de esta interfaz y la entidad no se fugue hacia
/// capas superiores.
/// </summary>
public interface IUsuarioService
{
    /// <summary>
    /// Da de alta un nuevo Usuario. Ademas reclama automaticamente cualquier
    /// AsignacionHerencia pendiente (invitacion sin cuenta) que lo nombraba por este
    /// mismo Email, dejandola vinculada a la cuenta recien creada.
    /// </summary>
    /// <exception cref="ReglaNegocioException">Datos invalidos o error tecnico.</exception>
    Task<UsuarioDTO> CrearUsuarioAsync(UsuarioCreacionDTO usuarioCreacionDTO);

    /// <summary>Busca un Usuario por su Id.</summary>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    /// <exception cref="ReglaNegocioException">Error tecnico al consultarlo.</exception>
    Task<UsuarioDTO> ObtenerUsuarioPorIdAsync(int id);

    /// <summary>Devuelve el listado completo de Usuarios registrados.</summary>
    /// <exception cref="ReglaNegocioException">Error tecnico al consultarlos.</exception>
    Task<IEnumerable<UsuarioDTO>> ObtenerTodosLosUsuariosAsync();

    /// <summary>Actualiza Nombre y Email de un Usuario existente.</summary>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    /// <exception cref="ReglaNegocioException">Datos invalidos o error tecnico al persistir.</exception>
    Task<UsuarioDTO> ActualizarUsuarioAsync(int id, UsuarioActualizacionDTO usuarioActualizacionDTO);

    /// <summary>
    /// Elimina un Usuario (y, por cascada en AppDbContext, sus ActivosDigitales como
    /// otorgante). Rechaza el borrado si el Usuario todavia tiene HerenciasRecibidas activas.
    /// </summary>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    /// <exception cref="ReglaNegocioException">Error tecnico al eliminarlo.</exception>
    Task EliminarUsuarioAsync(int id);

    /// <summary>
    /// Busca un Usuario por Email para el flujo de login de AuthController.
    /// </summary>
    /// <returns>
    /// El unico DTO de este servicio que incluye PasswordHash/PasswordSalt (ver
    /// UsuarioAutenticacionDTO), necesario para verificar la contraseña ingresada.
    /// </returns>
    /// <exception cref="RecursoNoEncontradoException">El email no corresponde a ningun Usuario.</exception>
    /// <exception cref="ReglaNegocioException">Error tecnico al consultarlo.</exception>
    Task<UsuarioAutenticacionDTO> ObtenerUsuarioParaAutenticacionAsync(string email);

    /// <summary>Cambia la contraseña de un Usuario ya autenticado que conoce su contraseña actual.</summary>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    /// <exception cref="ReglaNegocioException">
    /// "PasswordActual" no coincide con la persistida, "PasswordNueva" no cumple el largo
    /// minimo, o error tecnico al persistir.
    /// </exception>
    Task CambiarPasswordAsync(int usuarioId, CambiarPasswordDTO cambiarPasswordDTO);

    /// <summary>
    /// Primer paso del flujo de "olvide mi contraseña": genera un token de reseteo de un
    /// solo uso y vida corta, lo persiste con su expiracion, y lo devuelve en texto plano
    /// para que el controller lo "envie" (simulado) al Email del usuario.
    /// </summary>
    /// <returns>
    /// Null si el email no corresponde a ningun Usuario registrado. Es deliberado (mismo
    /// criterio anti "user enumeration" que ObtenerUsuarioParaAutenticacionAsync): el
    /// controller debe responder el mismo mensaje generico exista o no la cuenta.
    /// </returns>
    Task<string?> SolicitarResetPasswordAsync(string email);

    /// <summary>
    /// Segundo paso de "olvide mi contraseña": busca al Usuario por su
    /// PasswordResetToken vigente y, si no expiro, reemplaza la contraseña.
    /// </summary>
    /// <exception cref="ReglaNegocioException">El token no existe, ya expiro, o "PasswordNueva" no cumple el largo minimo.</exception>
    Task ResetearPasswordAsync(ResetearPasswordDTO resetearPasswordDTO);

    /// <summary>
    /// Primer paso del segundo factor de autenticacion: se invoca desde
    /// AuthController.Login tras validar la contraseña, si el Usuario tiene
    /// DobleFactorHabilitado=true. Genera un codigo de 6 digitos (CSPRNG), lo persiste con
    /// una ventana de vigencia corta, y lo envia por Email.
    /// </summary>
    /// <remarks>No devuelve el codigo: nadie fuera de este metodo necesita conocerlo en texto plano.</remarks>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    Task GenerarYEnviarCodigoDobleFactorAsync(int usuarioId);

    /// <summary>
    /// Segundo paso del segundo factor: compara el codigo ingresado contra el persistido
    /// en <see cref="GenerarYEnviarCodigoDobleFactorAsync"/>; si coincide y no expiro, lo
    /// invalida (uso unico) y devuelve el UsuarioDTO para que AuthController emita el JWT real.
    /// </summary>
    /// <exception cref="ReglaNegocioException">El codigo es invalido o ya expiro.</exception>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    Task<UsuarioDTO> VerificarCodigoDobleFactorAsync(int usuarioId, string codigo);

    /// <summary>
    /// Activa o desactiva el 2FA por email para la propia cuenta. Al desactivarlo, limpia
    /// cualquier codigo pendiente para no dejar un codigo vivo de un login a mitad de camino.
    /// </summary>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    Task<UsuarioDTO> ActualizarDobleFactorAsync(int usuarioId, bool habilitado);
}
