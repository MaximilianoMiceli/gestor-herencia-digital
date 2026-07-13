using System.ComponentModel.DataAnnotations;
using Herencia.Data.Models;

namespace Herencia.Business.Dtos;

// No incluye "UsuarioId": reasignar el dueño de un activo es una operacion de
// negocio distinta a editar sus datos (Nombre/Tipo/Descripcion).
public class ActivoDigitalActualizacionDTO
{
    [Required(ErrorMessage = "El nombre del activo es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    public TipoActivoDigital Tipo { get; set; }

    public string Descripcion { get; set; } = string.Empty;
}
