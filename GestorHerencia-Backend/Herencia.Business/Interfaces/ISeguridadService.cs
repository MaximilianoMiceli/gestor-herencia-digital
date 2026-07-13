namespace Herencia.Business.Interfaces;

/// <summary>
/// Contrato de criptografia de contraseñas, separado de IUsuarioService para que
/// cualquier flujo futuro reutilice el mismo algoritmo sin duplicarlo.
/// </summary>
public interface ISeguridadService
{
    /// <summary>Genera un par (hash, salt) nuevo a partir de una contraseña en texto plano.</summary>
    void CrearPasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt);

    /// <summary>Verifica si una contraseña en texto plano corresponde al hash+salt guardados para un usuario.</summary>
    bool VerificarPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt);
}
