using Herencia.Data.Models;

namespace Herencia.Data.Repositories;

/// <summary>Extiende el CRUD genérico con consultas específicas del dominio Usuario.</summary>
public interface IUsuarioRepository : IRepositorioBase<Usuario>
{
    /// <summary>
    /// Busca un Usuario por Email (login, y también usado por
    /// UsuarioService.CrearUsuarioAsync para reclamar invitaciones pendientes al registrarse).
    /// Devuelve null si el email no corresponde a ningún Usuario.
    /// </summary>
    Task<Usuario?> ObtenerPorEmailAsync(string email);

    /// <summary>
    /// Busca un Usuario por su PasswordResetToken vigente. Devuelve null si el token
    /// no existe (ya usado, nunca existió, o pertenece a otra cuenta).
    /// </summary>
    Task<Usuario?> ObtenerPorPasswordResetTokenAsync(string token);

    /// <summary>
    /// Busca un Usuario por DNI (columna con índice único). La usa
    /// UsuarioService.CrearUsuarioAsync para rechazar un registro duplicado con un
    /// mensaje claro, en vez de dejar que falle recién en el INSERT.
    /// </summary>
    Task<Usuario?> ObtenerPorDniAsync(string dni);
}
