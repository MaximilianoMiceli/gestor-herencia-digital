using Herencia.Data.Models;

namespace Herencia.Business.Dtos;

// ActivoDigitalDTO es el "contrato" de salida: lo que efectivamente se le
// devuelve a quien consulta un ActivoDigital. Al igual que UsuarioDTO, es una
// version "aplanada" y controlada de la entidad de Data: no incluye la
// propiedad de navegacion "Usuario" completa (que arrastraria de nuevo todos
// los datos sensibles del titular, como su PasswordHash, si no se tiene
// cuidado) ni la coleccion "AsignacionesHerencia". Solo exponemos "UsuarioId"
// como referencia simple.
public class ActivoDigitalDTO
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public TipoActivoDigital Tipo { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public int UsuarioId { get; set; }
}
