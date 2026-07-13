using Herencia.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Herencia.Data.Repositories;

/// <summary>Repositorio de Usuario: hereda el CRUD genérico y suma las consultas de <see cref="IUsuarioRepository"/>.</summary>
public class UsuarioRepository : RepositorioBase<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(AppDbContext contexto) : base(contexto)
    {
    }

    /// <inheritdoc />
    public async Task<Usuario?> ObtenerPorEmailAsync(string email)
    {
        return await _contexto.Usuarios
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <inheritdoc />
    public async Task<Usuario?> ObtenerPorPasswordResetTokenAsync(string token)
    {
        return await _contexto.Usuarios
            .FirstOrDefaultAsync(u => u.PasswordResetToken == token);
    }

    /// <inheritdoc />
    public async Task<Usuario?> ObtenerPorDniAsync(string dni)
    {
        return await _contexto.Usuarios
            .FirstOrDefaultAsync(u => u.Dni == dni);
    }
}
