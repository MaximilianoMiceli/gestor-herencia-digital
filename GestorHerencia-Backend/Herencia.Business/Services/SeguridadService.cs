using System.Security.Cryptography;
using System.Text;
using Herencia.Business.Interfaces;

namespace Herencia.Business.Services;

/// <summary>
/// Implementación de <see cref="ISeguridadService"/>: algoritmo criptográfico usado para
/// proteger las contraseñas de los usuarios. Servicio utilitario puro, sin dependencias
/// de persistencia, fácil de testear unitariamente.
/// </summary>
public class SeguridadService : ISeguridadService
{
    /// <summary>
    /// Calcula un hash y un salt nuevos para una contraseña en texto plano.
    /// </summary>
    public void CrearPasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        // HMACSHA512 (no SHA512 a secas) porque un hash determinístico permite rainbow
        // tables y ataques de diccionario offline masivos si dos usuarios comparten
        // contraseña. Sin clave explícita, HMACSHA512 genera una clave aleatoria de 128
        // bytes por instancia: esa clave (hmac.Key) es el salt.
        using var hmac = new HMACSHA512();

        // El salt es único por usuario y se guarda en texto plano junto al hash (su
        // seguridad no depende de ser secreto, sino de ser distinto por cuenta), lo que
        // vuelve inútiles las rainbow tables precalculadas.
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

        // CryptographicOperations.FixedTimeEquals en vez de SequenceEqual: una comparación
        // que corta en el primer byte distinto es vulnerable a un ataque de temporización
        // (timing attack); FixedTimeEquals siempre recorre todos los bytes.
        return CryptographicOperations.FixedTimeEquals(computedHash, passwordHash);
    }
}
