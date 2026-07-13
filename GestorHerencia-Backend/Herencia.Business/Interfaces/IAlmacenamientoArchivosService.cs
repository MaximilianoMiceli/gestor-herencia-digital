namespace Herencia.Business.Interfaces;

/// <summary>
/// Abstrae donde y como se guarda fisicamente un archivo subido. Recibe Stream + nombre
/// original en vez de <c>IFormFile</c> porque Herencia.Business no referencia el framework web.
/// </summary>
public interface IAlmacenamientoArchivosService
{
    /// <summary>Guarda "contenido" y devuelve la ruta/clave con la que quedo guardado (nunca el nombre original).</summary>
    /// <param name="subcarpeta">Separa archivos por tipo dentro del mismo almacen fisico.</param>
    Task<string> GuardarArchivoAsync(Stream contenido, string nombreArchivoOriginal, string subcarpeta = "");
}
