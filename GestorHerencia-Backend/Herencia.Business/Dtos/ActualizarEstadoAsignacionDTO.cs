using System.ComponentModel.DataAnnotations;
using Herencia.Data.Models;

namespace Herencia.Business.Dtos;

// DTO deliberadamente minimo (un solo campo): PATCH .../estado no debe poder
// colar cambios de Porcentaje/CondicionLiberacion como haria reusar
// AsignacionHerenciaActualizacionDTO.
public class ActualizarEstadoAsignacionDTO
{
    // [EnumDataType] rechaza enteros que no correspondan a ningun miembro real
    // del enum (ej: 99), algo que el model binding por si solo aceptaria.
    [Required(ErrorMessage = "El nuevo estado es obligatorio.")]
    [EnumDataType(typeof(EstadoBeneficiario), ErrorMessage = "El estado indicado no es un valor valido (use Aceptado o Rechazado).")]
    public EstadoBeneficiario NuevoEstado { get; set; }
}
