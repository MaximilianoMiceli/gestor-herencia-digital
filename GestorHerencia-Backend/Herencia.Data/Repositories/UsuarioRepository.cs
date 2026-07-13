using Herencia.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Herencia.Data.Repositories;

/// <summary>Repositorio de Usuario: hereda el CRUD genérico y suma las consultas de <see cref="IUsuarioRepository"/>.</summary>
public class UsuarioRepository : RepositorioBase<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(AppDbContext contexto) : base(contexto)
    {
    }

    /// <summary>Ver <see cref="IUsuarioRepository.ObtenerPorEmailAsync"/>.</summary>
    public async Task<Usuario?> ObtenerPorEmailAsync(string email)
    {
        // FirstOrDefaultAsync es correcto porque Email tiene índice único (AppDbContext.OnModelCreating):
        // como mucho hay un Usuario con ese valor exacto.
        return await _contexto.Usuarios
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <summary>Ver <see cref="IUsuarioRepository.ObtenerPorPasswordResetTokenAsync"/>.</summary>
    public async Task<Usuario?> ObtenerPorPasswordResetTokenAsync(string token)
    {
        return await _contexto.Usuarios
            .FirstOrDefaultAsync(u => u.PasswordResetToken == token);
    }

    /// <summary>Ver <see cref="IUsuarioRepository.ObtenerPorDniAsync"/>.</summary>
    public async Task<Usuario?> ObtenerPorDniAsync(string dni)
    {
        // Dni también tiene índice único: FirstOrDefaultAsync no puede devolver resultados ambiguos.
        return await _contexto.Usuarios
            .FirstOrDefaultAsync(u => u.Dni == dni);
    }
}
