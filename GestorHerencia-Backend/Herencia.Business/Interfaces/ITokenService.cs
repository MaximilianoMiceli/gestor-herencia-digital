using Herencia.Data.Models;

namespace Herencia.Business.Interfaces;

/// <summary>
/// Contrato para la emision de Tokens JWT una vez que un Usuario ya fue autenticado
/// (tipicamente tras <see cref="ISeguridadService.VerificarPasswordHash"/>). Se separa de
/// ISeguridadService porque son preocupaciones distintas: una verifica identidad, la otra
/// emite la credencial que la demuestra en requests futuros.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Genera un JWT firmado para la identidad del Usuario recibido.
    /// </summary>
    /// <remarks>
    /// Recibe la entidad Usuario completa (no un DTO) porque es un servicio interno de
    /// Business que nunca se expone a la Api: solo lee Id y Email para armar los Claims,
    /// nunca toca ni expone PasswordHash/PasswordSalt.
    /// </remarks>
    string CrearToken(Usuario usuario);
}
