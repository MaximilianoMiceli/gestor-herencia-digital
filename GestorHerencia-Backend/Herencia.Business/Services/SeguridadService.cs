using System.Security.Cryptography;
using System.Text;
using Herencia.Business.Interfaces;

namespace Herencia.Business.Services;

/// <summary>
/// Implementación de <see cref="ISeguridadService"/>: algoritmo criptográfico usado
/// para proteger las contraseñas de los usuarios.
/// </summary>
public class SeguridadService : ISeguridadService
{
    /// <summary>
    /// Calcula un hash y un salt nuevos para una contraseña en texto plano.
    /// </summary>
    public void CrearPasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        // HMACSHA512 (no SHA512 a secas) evita rainbow tables: sin clave explícita genera
        // una clave aleatoria de 128 bytes por instancia, que se usa como salt.
        using var hmac = new HMACSHA512();

        // El salt es único por usuario; su seguridad no depende de ser secreto sino distinto.
        passwordSalt = hmac.Key;

        passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    }

    /// <summary>
    /// Recalcula el hash de una contraseña candidata con el mismo salt original y lo
    /// compara, en tiempo constante, contra el hash guardado.
    /// </summary>
    public bool VerificarPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
    {
        using var hmac = new HMACSHA512(passwordSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

        // FixedTimeEquals en vez de SequenceEqual: evita un timing attack al comparar.
        return CryptographicOperations.FixedTimeEquals(computedHash, passwordHash);
    }
}
