using System.ComponentModel.DataAnnotations;

namespace Herencia.Business.Dtos;

// AsignacionHerenciaCreacionDTO representa UN elemento del lote de
// asignaciones que se crea en POST /api/activosdigitales/{id}/asignaciones.
// Notar que NO incluye "ActivoDigitalId": ese dato viaja en la URL (el "{id}"
// de la ruta anidada), no en el body, porque conceptualmente se esta creando
// el "detalle" de un "maestro" ya identificado por la ruta.
//
// --- ¿Por que "EmailBeneficiario" y no "UsuarioBeneficiarioId"? ---
// Con el modelo de doble rol, el otorgante ya NO elige a un Beneficiario de
// una lista de contactos propios (como antes, con "BeneficiarioId"): invita
// a una persona por su EMAIL, que puede o no corresponder todavia a una
// cuenta de Usuario existente. Es tarea de AsignacionHerenciaService (no de
// este DTO ni del controller) resolver, en el momento de la creacion, si ese
// email ya pertenece a un Usuario registrado (y completar UsuarioId de una)
// o si hay que dejarlo pendiente de reclamo (UsuarioId null) hasta que esa
// persona se registre.
public class AsignacionHerenciaCreacionDTO
{
    [Required(ErrorMessage = "El email del beneficiario es obligatorio.")]
    [EmailAddress(ErrorMessage = "El email del beneficiario no tiene un formato valido.")]
    public string EmailBeneficiario { get; set; } = string.Empty;

    // Porcentaje del activo que le corresponde a este beneficiario. Se
    // valida (en el servicio) que este en el rango (0, 100], y que la SUMA
    // de porcentajes de un mismo ActivoDigital nunca supere el 100%.
    [Range(0.01, 100, ErrorMessage = "El porcentaje asignado debe ser mayor a 0 y menor o igual a 100.")]
    public decimal PorcentajeAsignado { get; set; }

    public string CondicionLiberacion { get; set; } = string.Empty;
}
