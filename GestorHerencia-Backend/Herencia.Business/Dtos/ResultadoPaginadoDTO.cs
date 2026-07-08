namespace Herencia.Business.Dtos;

// ResultadoPaginadoDTO<T> es un "envoltorio" (wrapper) GENERICO para cualquier
// respuesta paginada de la Api. Se hace generico (en vez de crear una clase
// "ActivoDigitalPaginadoDTO" especifica) porque el CONCEPTO de paginacion
// (una porcion de resultados + metadatos sobre el total) es independiente de
// QUE tipo de dato se este paginando: el dia de mañana, si se pagina el
// listado de Beneficiarios o de Usuarios, se reutiliza esta misma clase
// cambiando unicamente el parametro de tipo T.
public class ResultadoPaginadoDTO<T>
{
    // Los elementos de la pagina ACTUAL unicamente (no la coleccion completa).
    public IEnumerable<T> Items { get; set; } = [];

    // Numero de pagina que efectivamente se devolvio (ya normalizado/acotado
    // por el servicio, ver ActivoDigitalService.ObtenerActivosPorUsuarioPaginadoAsync).
    public int PaginaActual { get; set; }

    // Cantidad maxima de registros por pagina que efectivamente se aplico.
    public int RegistrosPorPagina { get; set; }

    // Cantidad TOTAL de registros que matchean el filtro, SIN paginar (ej: el
    // usuario tiene 37 activos digitales en total).
    public int TotalRegistros { get; set; }

    // Cantidad total de paginas disponibles, calculada como
    // ceil(TotalRegistros / RegistrosPorPagina). Se expone ya calculada para
    // que el cliente de la Api (ej: un frontend) no tenga que rehacer esa
    // cuenta por su cuenta.
    public int TotalPaginas { get; set; }
}
