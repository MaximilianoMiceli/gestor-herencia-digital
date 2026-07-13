using Herencia.Business.Dtos;

namespace Herencia.Business.Interfaces;

/// <summary>
/// Contrato de la logica de negocio de Usuario. Trabaja solo con DTOs para que la
/// entidad Usuario nunca se fugue hacia capas superiores.
/// </summary>
public interface IUsuarioService
{
    /// <summary>Da de alta un nuevo Usuario y reclama cualquier invitacion pendiente a su Email.</summary>
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

    /// <summary>Elimina un Usuario (cascade borra sus ActivosDigitales); rechaza si tiene HerenciasRecibidas activas.</summary>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    /// <exception cref="ReglaNegocioException">Error tecnico al eliminarlo.</exception>
    Task EliminarUsuarioAsync(int id);

    /// <summary>Busca un Usuario por Email para el login; el unico DTO que incluye PasswordHash/PasswordSalt.</summary>
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
    /// Primer paso de "olvide mi contraseña": genera un token de reseteo de un solo uso
    /// y vida corta.
    /// </summary>
    /// <returns>Null si el email no existe (deliberado, anti "user enumeration").</returns>
    Task<string?> SolicitarResetPasswordAsync(string email);

    /// <summary>Segundo paso: busca al Usuario por PasswordResetToken vigente y reemplaza la contraseña.</summary>
    /// <exception cref="ReglaNegocioException">El token no existe, ya expiro, o "PasswordNueva" no cumple el largo minimo.</exception>
    Task ResetearPasswordAsync(ResetearPasswordDTO resetearPasswordDTO);

    /// <summary>Genera un codigo de 6 digitos (CSPRNG) para el segundo factor y lo envia por Email.</summary>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    Task GenerarYEnviarCodigoDobleFactorAsync(int usuarioId);

    /// <summary>
    /// Compara el codigo contra <see cref="GenerarYEnviarCodigoDobleFactorAsync"/>; si
    /// coincide y no expiro, lo invalida (uso unico) y devuelve el UsuarioDTO.
    /// </summary>
    /// <exception cref="ReglaNegocioException">El codigo es invalido o ya expiro.</exception>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    Task<UsuarioDTO> VerificarCodigoDobleFactorAsync(int usuarioId, string codigo);

    /// <summary>Activa/desactiva el 2FA por email; al desactivar, limpia cualquier codigo pendiente.</summary>
    /// <exception cref="RecursoNoEncontradoException">El Id no existe.</exception>
    Task<UsuarioDTO> ActualizarDobleFactorAsync(int usuarioId, bool habilitado);
}
