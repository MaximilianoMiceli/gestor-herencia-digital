namespace Herencia.Business.Interfaces;

/// <summary>
/// Abstrae donde y como se guarda fisicamente un archivo subido. Recibe un Stream +
/// nombre original en vez de <c>IFormFile</c> a proposito: ese tipo vive en el framework
/// web y Herencia.Business es una libreria de clases pura que no lo referencia (misma
/// separacion de capas que ya respeta el resto del proyecto para AppDbContext/EF Core).
/// El controller (que si conoce IFormFile) abre el Stream antes de llamar al servicio.
/// </summary>
public interface IAlmacenamientoArchivosService
{
    /// <summary>
    /// Guarda "contenido" de forma permanente y devuelve la ruta/clave con la que quedo
    /// guardado (nunca el nombre original).
    /// </summary>
    /// <param name="subcarpeta">
    /// Permite a cada llamador separar sus archivos dentro del mismo almacen fisico sin
    /// necesitar una implementacion nueva por tipo de archivo. Vacio (default) preserva el
    /// comportamiento historico de guardar directo en la carpeta base configurada.
    /// </param>
    Task<string> GuardarArchivoAsync(Stream contenido, string nombreArchivoOriginal, string subcarpeta = "");
}
