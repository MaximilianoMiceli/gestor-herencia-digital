using System.ComponentModel.DataAnnotations;
using Herencia.Data.Models;

namespace Herencia.Business.Dtos;

// No expone "Id", las propiedades de auditoria ni la coleccion de navegacion
// "AsignacionesHerencia": esos datos los completa el propio sistema, no quien
// llama a la Api.
public class ActivoDigitalCreacionDTO
{
    [Required(ErrorMessage = "El nombre del activo es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    public TipoActivoDigital Tipo { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    // ActivoDigitalService valida que este Usuario exista antes de crear el activo.
    public int UsuarioId { get; set; }
}
