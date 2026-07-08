using System.ComponentModel.DataAnnotations;
using Herencia.Data.Models;

namespace Herencia.Business.Dtos;

// ActivoDigitalActualizacionDTO es el "contrato" de entrada para un PUT
// /api/activosdigitales/{id}. Deliberadamente NO incluye "UsuarioId": el titular
// de un activo digital no deberia poder "transferirse" con una simple edicion de
// datos (Nombre/Tipo/Descripcion); reasignar el dueño de un activo es una
// operacion de negocio mas delicada que queda fuera del alcance de esta etapa.
// Al no exponer esa propiedad aca, ni siquiera es posible que un cliente de la
// Api intente modificarla por este endpoint.
public class ActivoDigitalActualizacionDTO
{
    [Required(ErrorMessage = "El nombre del activo es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    public TipoActivoDigital Tipo { get; set; }

    public string Descripcion { get; set; } = string.Empty;
}
