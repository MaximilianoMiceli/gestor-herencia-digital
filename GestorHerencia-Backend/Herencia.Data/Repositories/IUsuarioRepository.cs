using Herencia.Data.Models;

namespace Herencia.Data.Repositories;

/// <summary>Extiende el CRUD genérico con consultas específicas del dominio Usuario.</summary>
public interface IUsuarioRepository : IRepositorioBase<Usuario>
{
    /// <summary>Busca un Usuario por Email (login, y para reclamar invitaciones pendientes al registrarse).</summary>
    Task<Usuario?> ObtenerPorEmailAsync(string email);

    /// <summary>Busca un Usuario por su PasswordResetToken vigente. Null si no existe o ya fue usado.</summary>
    Task<Usuario?> ObtenerPorPasswordResetTokenAsync(string token);

    /// <summary>Busca un Usuario por DNI (columna con índice único), para rechazar registros duplicados con un mensaje claro.</summary>
    Task<Usuario?> ObtenerPorDniAsync(string dni);
}
