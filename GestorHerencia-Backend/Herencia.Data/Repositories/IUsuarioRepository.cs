using Herencia.Data.Models;

namespace Herencia.Data.Repositories;

// IUsuarioRepository extiende el contrato generico IRepositorioBase<Usuario> para
// sumar operaciones de consulta ESPECIFICAS del dominio de Usuario, que no tendria
// sentido meter dentro del repositorio generico (porque no aplican a cualquier T).
public interface IUsuarioRepository : IRepositorioBase<Usuario>
{
    // Busca un Usuario por su Email (en vez de por Id). Es la consulta que
    // necesita el flujo de LOGIN: el cliente se identifica con su email, no
    // con su Id de base de datos. Tambien es la consulta que usa
    // UsuarioService.CrearUsuarioAsync para "reclamar" automaticamente, al
    // registrarse, cualquier AsignacionHerencia pendiente que lo invito por
    // este mismo email (ver AsignacionHerencia.EmailInvitado). Devuelve
    // "Usuario?" porque el email ingresado podria no corresponder a ningun
    // Usuario registrado.
    Task<Usuario?> ObtenerPorEmailAsync(string email);

    // Busca un Usuario por su PasswordResetToken vigente (ver
    // UsuarioService.ResetearPasswordAsync). Devuelve "Usuario?" porque el
    // token podria no existir (ya fue usado, nunca existio, o pertenece a
    // otra cuenta).
    Task<Usuario?> ObtenerPorPasswordResetTokenAsync(string token);

    // Busca un Usuario por su DNI (columna con indice UNICO, ver
    // AppDbContext.OnModelCreating). La usa UsuarioService.CrearUsuarioAsync
    // para rechazar, con un mensaje claro, un intento de registro con un DNI
    // que ya pertenece a otra cuenta, en vez de dejar que la violacion del
    // indice UNICO se descubra recien al intentar el INSERT (lo que
    // terminaba devolviendo un mensaje generico sin explicar el motivo real).
    Task<Usuario?> ObtenerPorDniAsync(string dni);
}
