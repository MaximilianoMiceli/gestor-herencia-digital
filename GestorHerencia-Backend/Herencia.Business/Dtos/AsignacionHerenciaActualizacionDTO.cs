using System.ComponentModel.DataAnnotations;

namespace Herencia.Business.Dtos;

// AsignacionHerenciaActualizacionDTO: contrato de entrada para modificar el
// Porcentaje/Condicion de UNA asignacion ya existente. No permite reasignar
// ActivoDigitalId ni BeneficiarioId (eso equivaldria a "otra" asignacion
// distinta; se borraria y crearia de nuevo, no se "edita").
public class AsignacionHerenciaActualizacionDTO
{
    [Range(0.01, 100, ErrorMessage = "El porcentaje asignado debe ser mayor a 0 y menor o igual a 100.")]
    public decimal PorcentajeAsignado { get; set; }

    public string CondicionLiberacion { get; set; } = string.Empty;
}
