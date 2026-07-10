using Herencia.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Herencia.Data.Repositories;

// AsignacionHerenciaRepository hereda el CRUD generico (incluida
// EjecutarEnTransaccionAsync) de RepositorioBase<AsignacionHerencia> y suma
// las consultas especificas de esta entidad de asociacion N-N.
public class AsignacionHerenciaRepository : RepositorioBase<AsignacionHerencia>, IAsignacionHerenciaRepository
{
    public AsignacionHerenciaRepository(AppDbContext contexto) : base(contexto)
    {
    }

    // ObtenerPorActivoDigitalAsync: todas las asignaciones de un ActivoDigital.
    public async Task<IEnumerable<AsignacionHerencia>> ObtenerPorActivoDigitalAsync(int activoDigitalId)
    {
        return await _contexto.AsignacionesHerencia
            .Where(a => a.ActivoDigitalId == activoDigitalId)
            .ToListAsync();
    }

    // ObtenerConActivoDigitalAsync: busca una asignacion por Id, con su
    // ActivoDigital ya cargado (Include), para poder leer
    // asignacion.ActivoDigital.UsuarioId sin una consulta adicional aparte.
    public async Task<AsignacionHerencia?> ObtenerConActivoDigitalAsync(int id)
    {
        return await _contexto.AsignacionesHerencia
            .Include(a => a.ActivoDigital)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    // ObtenerPorUsuarioBeneficiarioAsync: todas las asignaciones donde este
    // Usuario participa como BENEFICIARIO, con su ActivoDigital ya cargado
    // (Include) para poder mostrar nombre/tipo del activo heredado.
    public async Task<IEnumerable<AsignacionHerencia>> ObtenerPorUsuarioBeneficiarioAsync(int usuarioId)
    {
        return await _contexto.AsignacionesHerencia
            .Include(a => a.ActivoDigital)
            .Where(a => a.UsuarioId == usuarioId)
            .ToListAsync();
    }

    // ObtenerPendientesPorEmailAsync: invitaciones todavia SIN reclamar
    // (UsuarioId == null) que coinciden con el email indicado.
    public async Task<IEnumerable<AsignacionHerencia>> ObtenerPendientesPorEmailAsync(string email)
    {
        // ToLower() en ambos lados de la comparacion: el email ingresado al
        // registrarse podria diferir en mayusculas/minusculas del que el
        // otorgante tipeo originalmente al invitar (ej: "Ana@Mail.com" vs
        // "ana@mail.com" son, para cualquier persona, el mismo casillero de
        // correo). SQLite compara strings de forma "case-sensitive" por
        // defecto para comparaciones de igualdad simples, por lo que
        // normalizamos explicitamente ambos lados en vez de asumir que la
        // base de datos lo hace por nosotros.
        var emailNormalizado = email.Trim().ToLower();

        return await _contexto.AsignacionesHerencia
            .Where(a => a.UsuarioId == null && a.EmailInvitado.ToLower() == emailNormalizado)
            .ToListAsync();
    }

    // ObtenerPorTokenInvitacionAsync: busca por el identificador PUBLICO no
    // adivinable, con su ActivoDigital ya cargado (Include).
    public async Task<AsignacionHerencia?> ObtenerPorTokenInvitacionAsync(string token)
    {
        return await _contexto.AsignacionesHerencia
            .Include(a => a.ActivoDigital)
            .FirstOrDefaultAsync(a => a.TokenInvitacion == token);
    }
}
