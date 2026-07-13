using Herencia.Business.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Herencia.Business.Services;

/// <summary>
/// Única implementación de <see cref="IAlmacenamientoArchivosService"/>: guarda los
/// archivos en una carpeta del disco local del servidor.
/// </summary>
public class AlmacenamientoLocalService : IAlmacenamientoArchivosService
{
    // Carpeta raíz compartida; cada tipo de archivo vive en su propia subcarpeta.
    private readonly string _carpetaDestino;

    public AlmacenamientoLocalService(IConfiguration configuration)
    {
        var carpetaConfigurada = configuration["Almacenamiento:CarpetaRaiz"] ?? "uploads";

        // Si es relativa, se resuelve contra AppContext.BaseDirectory para que sea estable
        // sin importar desde dónde se invoque el proceso.
        _carpetaDestino = Path.IsPathRooted(carpetaConfigurada)
            ? carpetaConfigurada
            : Path.Combine(AppContext.BaseDirectory, carpetaConfigurada);
    }

    /// <summary>
    /// Guarda el contenido recibido en disco bajo un nombre generado (nunca el original)
    /// y devuelve la ruta física resultante.
    /// </summary>
    public async Task<string> GuardarArchivoAsync(Stream contenido, string nombreArchivoOriginal, string subcarpeta = "")
    {
        var carpetaDestinoFinal = string.IsNullOrWhiteSpace(subcarpeta)
            ? _carpetaDestino
            : Path.Combine(_carpetaDestino, subcarpeta);

        Directory.CreateDirectory(carpetaDestinoFinal);

        // El nombre original no es confiable (path traversal, colisiones): se genera uno
        // nuevo con Guid.NewGuid() y el original se conserva solo como metadato.
        var extension = Path.GetExtension(nombreArchivoOriginal);
        var nombreEnDisco = $"{Guid.NewGuid():N}{extension}";
        var rutaCompleta = Path.Combine(carpetaDestinoFinal, nombreEnDisco);

        await using (var destino = new FileStream(rutaCompleta, FileMode.CreateNew, FileAccess.Write))
        {
            await contenido.CopyToAsync(destino);
        }

        return rutaCompleta;
    }
}
