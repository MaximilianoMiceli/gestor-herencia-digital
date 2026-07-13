using System.ComponentModel.DataAnnotations;

namespace Herencia.Business.Dtos;

// No permite reasignar ActivoDigitalId ni el beneficiario (seria otra
// asignacion, no una edicion) ni tocar el Estado (endpoint propio, ver
// ActualizarEstadoAsignacionDTO).
public class AsignacionHerenciaActualizacionDTO
{
    [Range(0.01, 100, ErrorMessage = "El porcentaje asignado debe ser mayor a 0 y menor o igual a 100.")]
    public decimal PorcentajeAsignado { get; set; }

    public string CondicionLiberacion { get; set; } = string.Empty;
}
