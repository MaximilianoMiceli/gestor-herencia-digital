using Herencia.Data.Models;

namespace Herencia.Data.Repositories;

/// <summary>Extiende el CRUD genérico con las consultas propias de la relación N-N ActivoDigital/Usuario(Beneficiario).</summary>
public interface IAsignacionHerenciaRepository : IRepositorioBase<AsignacionHerencia>
{
    /// <summary>Devuelve todas las AsignacionHerencia de un ActivoDigital puntual.</summary>
    Task<IEnumerable<AsignacionHerencia>> ObtenerPorActivoDigitalAsync(int activoDigitalId);

    /// <summary>Busca una AsignacionHerencia por Id con su ActivoDigital cargado (Include), para resolver ownership en el controller.</summary>
    Task<AsignacionHerencia?> ObtenerConActivoDigitalAsync(int id);

    /// <summary>Devuelve las AsignacionHerencia donde el Usuario participa como Beneficiario, con su ActivoDigital cargado.</summary>
    Task<IEnumerable<AsignacionHerencia>> ObtenerPorUsuarioBeneficiarioAsync(int usuarioId);

    /// <summary>Devuelve las AsignacionHerencia sin reclamar (UsuarioId null) cuyo EmailInvitado coincide, sin distinguir mayúsculas/minúsculas.</summary>
    Task<IEnumerable<AsignacionHerencia>> ObtenerPendientesPorEmailAsync(string email);

    /// <summary>Busca una AsignacionHerencia por su TokenInvitacion (identificador público), con su ActivoDigital cargado.</summary>
    Task<AsignacionHerencia?> ObtenerPorTokenInvitacionAsync(string token);

    /// <summary>
    /// Devuelve las AsignacionHerencia ya Aceptadas cuyo ActivoDigital pertenece al Usuario otorgante
    /// indicado, con su Usuario beneficiario cargado.
    /// </summary>
    Task<IEnumerable<AsignacionHerencia>> ObtenerAceptadasPorOtorganteAsync(int usuarioOtorganteId);
}
