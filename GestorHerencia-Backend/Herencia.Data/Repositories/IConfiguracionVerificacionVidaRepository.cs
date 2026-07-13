using Herencia.Data.Models;

namespace Herencia.Data.Repositories;

/// <summary>Extiende el CRUD genérico con las consultas propias de este dominio.</summary>
public interface IConfiguracionVerificacionVidaRepository : IRepositorioBase<ConfiguracionVerificacionVida>
{
    /// <summary>Busca la configuración de un titular. Devuelve null si nunca configuró el monitoreo.</summary>
    Task<ConfiguracionVerificacionVida?> ObtenerPorUsuarioIdAsync(int usuarioId);

    /// <summary>Devuelve todas las configuraciones con Activo == true, usada por el background service en cada tick.</summary>
    Task<IEnumerable<ConfiguracionVerificacionVida>> ObtenerActivasParaEscaneoAsync();
}
