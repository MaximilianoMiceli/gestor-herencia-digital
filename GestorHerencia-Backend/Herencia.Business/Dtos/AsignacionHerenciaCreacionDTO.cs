using System.ComponentModel.DataAnnotations;

namespace Herencia.Business.Dtos;

// AsignacionHerenciaCreacionDTO representa UN elemento del lote de
// asignaciones que se crea en POST /api/activosdigitales/{id}/asignaciones.
// Notar que NO incluye "ActivoDigitalId": ese dato viaja en la URL (el "{id}"
// de la ruta anidada), no en el body, porque conceptualmente se esta creando
// el "detalle" de un "maestro" ya identificado por la ruta.
public class AsignacionHerenciaCreacionDTO
{
    [Required(ErrorMessage = "El Id del beneficiario es obligatorio.")]
    public int BeneficiarioId { get; set; }

    // Porcentaje del activo que le corresponde a este beneficiario. Se
    // valida (en el servicio) que este en el rango (0, 100], y que la SUMA
    // de porcentajes de un mismo ActivoDigital nunca supere el 100%.
    [Range(0.01, 100, ErrorMessage = "El porcentaje asignado debe ser mayor a 0 y menor o igual a 100.")]
    public decimal PorcentajeAsignado { get; set; }

    public string CondicionLiberacion { get; set; } = string.Empty;
}
