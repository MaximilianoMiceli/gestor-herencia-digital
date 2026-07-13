namespace Herencia.Business.Dtos;

// Generico para reutilizarse en cualquier listado paginado (activos,
// asignaciones, usuarios, etc.) sin crear una clase por tipo.
public class ResultadoPaginadoDTO<T>
{
    public IEnumerable<T> Items { get; set; } = [];

    // Ya normalizada/acotada por el servicio.
    public int PaginaActual { get; set; }

    public int RegistrosPorPagina { get; set; }

    public int TotalRegistros { get; set; }

    // ceil(TotalRegistros / RegistrosPorPagina), calculada para que el cliente no la rehaga.
    public int TotalPaginas { get; set; }
}
