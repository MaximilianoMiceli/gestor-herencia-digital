using Herencia.Business.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Herencia.Business.Services;

// AlmacenamientoLocalService es la unica implementacion de
// IAlmacenamientoArchivosService por ahora: guarda los archivos en una
// carpeta del disco local del servidor. Sigue el MISMO criterio que ya usa
// Program.cs para resolver la ruta del archivo SQLite: si la carpeta
// configurada ("VerificacionVida:CarpetaCertificados") es RELATIVA, se
// resuelve contra AppContext.BaseDirectory (la carpeta donde estan los
// binarios en ejecucion), para que sea estable sin importar desde donde se
// invoque el proceso.
public class AlmacenamientoLocalService : IAlmacenamientoArchivosService
{
    private readonly string _carpetaDestino;

    public AlmacenamientoLocalService(IConfiguration configuration)
    {
        var carpetaConfigurada = configuration["VerificacionVida:CarpetaCertificados"] ?? "certificados_defuncion";

        _carpetaDestino = Path.IsPathRooted(carpetaConfigurada)
            ? carpetaConfigurada
            : Path.Combine(AppContext.BaseDirectory, carpetaConfigurada);
    }

    public async Task<string> GuardarArchivoAsync(Stream contenido, string nombreArchivoOriginal)
    {
        Directory.CreateDirectory(_carpetaDestino);

        // --- Nunca se reutiliza el nombre original en disco ---
        // Un nombre de archivo elegido por el CLIENTE (ej: "../../etc/passwd",
        // o simplemente un nombre que colisiona con el de otro certificado
        // ya subido) es un dato NO CONFIABLE. Generar un nombre nuevo con
        // Guid.NewGuid() elimina de raiz tanto la posibilidad de un ataque
        // de path traversal como la de sobreescribir sin querer un archivo
        // ya existente; el nombre original se preserva aparte, solo como
        // metadato de exhibicion (ver CertificadoDefuncion.NombreArchivoOriginal).
        var extension = Path.GetExtension(nombreArchivoOriginal);
        var nombreEnDisco = $"{Guid.NewGuid():N}{extension}";
        var rutaCompleta = Path.Combine(_carpetaDestino, nombreEnDisco);

        await using (var destino = new FileStream(rutaCompleta, FileMode.CreateNew, FileAccess.Write))
        {
            await contenido.CopyToAsync(destino);
        }

        return rutaCompleta;
    }
}
