using Herencia.Data.Models;

namespace Herencia.Business.Dtos;

// No expone la navegacion "Usuario" completa (arrastraria datos sensibles del
// titular, como su PasswordHash) ni "AsignacionesHerencia"; solo "UsuarioId".
public class ActivoDigitalDTO
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public TipoActivoDigital Tipo { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public int UsuarioId { get; set; }

    // Nunca se expone "RutaArchivo": es un detalle interno del servidor.
    public string? NombreArchivoOriginal { get; set; }
}
