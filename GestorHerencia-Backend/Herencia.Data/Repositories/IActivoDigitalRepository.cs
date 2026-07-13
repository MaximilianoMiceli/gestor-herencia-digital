using Herencia.Data.Models;

namespace Herencia.Data.Repositories;

/// <summary>Extiende el CRUD genérico con consultas específicas del dominio ActivoDigital.</summary>
public interface IActivoDigitalRepository : IRepositorioBase<ActivoDigital>
{
    /// <summary>Devuelve todos los ActivosDigitales de un Usuario puntual (puede ser ninguno).</summary>
    Task<IEnumerable<ActivoDigital>> ObtenerActivosPorUsuarioAsync(int usuarioId);

    /// <summary>
    /// Versión paginada y filtrada (opcionalmente por <paramref name="tipo"/> y/o
    /// <paramref name="nombre"/>) de <see cref="ObtenerActivosPorUsuarioAsync"/>. Devuelve además
    /// el total de registros que matchean los filtros (sin paginar), necesario para calcular la
    /// cantidad de páginas del lado del cliente. Se devuelve como tupla (Items, Total) para no
    /// tener que consultar la base de datos dos veces por separado desde el llamador.
    /// </summary>
    Task<(IEnumerable<ActivoDigital> Items, int Total)> ObtenerActivosPorUsuarioPaginadoAsync(
        int usuarioId, int pagina, int limite, TipoActivoDigital? tipo, string? nombre);
}
