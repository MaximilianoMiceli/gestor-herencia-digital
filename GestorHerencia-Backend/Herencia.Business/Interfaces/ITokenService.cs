using Herencia.Data.Models;

namespace Herencia.Business.Interfaces;

/// <summary>
/// Contrato para la emision de Tokens JWT una vez autenticado el Usuario. Separado de
/// ISeguridadService: uno verifica identidad, el otro emite la credencial.
/// </summary>
public interface ITokenService
{
    /// <summary>Genera un JWT firmado para la identidad del Usuario recibido.</summary>
    string CrearToken(Usuario usuario);
}
