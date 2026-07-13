namespace Herencia.Business.Interfaces;

/// <summary>
/// Contrato de criptografia de contraseñas. Separado de IUsuarioService (responsabilidad
/// unica: hashear/verificar es una preocupacion de seguridad, no una regla de negocio de
/// Usuario) para que cualquier otro flujo futuro (cambio/recupero de contraseña) reutilice
/// el mismo algoritmo en vez de duplicarlo. Es un servicio utilitario puro: no depende de
/// IUsuarioRepository, AppDbContext ni ninguna infraestructura.
/// </summary>
public interface ISeguridadService
{
    /// <summary>
    /// Genera un par (hash, salt) nuevo a partir de una contraseña en texto plano.
    /// </summary>
    /// <remarks>
    /// Usa parametros "out" (en vez de una tupla) porque es la firma exacta pedida por la
    /// rubrica, y deja explicito que ambas salidas son igual de necesarias: no tiene
    /// sentido persistir el hash sin el salt, ni el salt sin el hash.
    /// </remarks>
    void CrearPasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt);

    /// <summary>Verifica si una contraseña en texto plano corresponde al hash+salt guardados para un usuario.</summary>
    bool VerificarPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt);
}
