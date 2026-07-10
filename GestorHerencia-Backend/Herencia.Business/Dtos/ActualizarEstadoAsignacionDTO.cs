using System.ComponentModel.DataAnnotations;
using Herencia.Data.Models;

namespace Herencia.Business.Dtos;

// ActualizarEstadoAsignacionDTO es el "contrato" de entrada del endpoint
// PATCH api/asignaciones/{id}/estado. Es un DTO deliberadamente MINIMO (un
// solo campo) porque una actualizacion PARCIAL solo deberia poder tocar,
// justamente, el dato que le da nombre a la operacion: el Estado. Reusar
// AsignacionHerenciaActualizacionDTO (que trae Porcentaje/CondicionLiberacion)
// permitiria, por error o por un cliente malicioso, colar cambios de otros
// campos "de arriba" en una operacion que semanticamente solo deberia poder
// aceptar o rechazar una herencia.
public class ActualizarEstadoAsignacionDTO
{
    // [EnumDataType] valida que el valor recibido en el JSON corresponda a
    // alguno de los miembros REALMENTE definidos en el enum (1, 2 o 3). Sin
    // esta validacion, el model binding de ASP.NET Core aceptaria igual, por
    // ejemplo, el entero "99" (que no representa ningun estado real) y lo
    // dejaria pasar hasta la capa de negocio como si fuera un valor legitimo
    // del enum, que es exactamente el tipo de dato invalido que un enum
    // deberia impedir en primer lugar.
    [Required(ErrorMessage = "El nuevo estado es obligatorio.")]
    [EnumDataType(typeof(EstadoBeneficiario), ErrorMessage = "El estado indicado no es un valor valido (use Aceptado o Rechazado).")]
    public EstadoBeneficiario NuevoEstado { get; set; }
}
