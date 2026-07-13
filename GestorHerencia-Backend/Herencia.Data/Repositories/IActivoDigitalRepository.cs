using Herencia.Data.Models;

namespace Herencia.Data.Repositories;

/// <summary>Extiende el CRUD genérico con consultas específicas del dominio ActivoDigital.</summary>
public interface IActivoDigitalRepository : IRepositorioBase<ActivoDigital>
{
    /// <summary>Devuelve todos los ActivosDigitales de un Usuario puntual (puede ser ninguno).</summary>
    Task<IEnumerable<ActivoDigital>> ObtenerActivosPorUsuarioAsync(int usuarioId);

    /// <summary>
    /// Version paginada y filtrada (por tipo y/o nombre) de <see cref="ObtenerActivosPorUsuarioAsync"/>.
    /// Devuelve tambien el total sin paginar, para calcular la cantidad de paginas en el cliente.
    /// </summary>
    Task<(IEnumerable<ActivoDigital> Items, int Total)> ObtenerActivosPorUsuarioPaginadoAsync(
        int usuarioId, int pagina, int limite, TipoActivoDigital? tipo, string? nombre);
}
