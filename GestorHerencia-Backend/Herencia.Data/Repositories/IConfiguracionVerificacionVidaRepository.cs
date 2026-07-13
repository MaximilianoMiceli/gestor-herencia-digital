using Herencia.Data.Models;

namespace Herencia.Data.Repositories;

/// <summary>
/// Extiende el CRUD genérico con las consultas propias de este dominio. Notar que
/// ObtenerPorIdAsync (heredado) ya equivale a "buscar por usuario", porque la PK de esta tabla
/// es UsuarioId (clave compartida); se agrega igual ObtenerPorUsuarioIdAsync por legibilidad
/// del lado de Business.
/// </summary>
public interface IConfiguracionVerificacionVidaRepository : IRepositorioBase<ConfiguracionVerificacionVida>
{
    /// <summary>Busca la configuración de un titular. Devuelve null si nunca configuró el monitoreo.</summary>
    Task<ConfiguracionVerificacionVida?> ObtenerPorUsuarioIdAsync(int usuarioId);

    /// <summary>
    /// Devuelve todas las configuraciones con Activo == true: la consulta que usa
    /// VerificacionVidaBackgroundService en cada tick para evaluar vencimientos.
    /// </summary>
    Task<IEnumerable<ConfiguracionVerificacionVida>> ObtenerActivasParaEscaneoAsync();
}
