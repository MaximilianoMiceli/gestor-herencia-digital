namespace Herencia.Business.Interfaces;

// IAlmacenamientoArchivosService abstrae DONDE y COMO se guarda fisicamente
// un archivo subido (hoy, unicamente los certificados de defuncion). Recibe
// un Stream + nombre original en vez de "Microsoft.AspNetCore.Http.IFormFile"
// a proposito: IFormFile es un tipo del FRAMEWORK WEB (vive en el paquete
// compartido de ASP.NET Core), y Herencia.Business es una libreria de
// clases "pura" que no referencia ese framework (ver Herencia.Business.csproj:
// Sdk="Microsoft.NET.Sdk", no "Microsoft.NET.Sdk.Web"). Acoplar la capa de
// negocio a un tipo de la capa Api rompería la misma separacion de capas
// que el resto del proyecto respeta para AppDbContext/EF Core. El
// controller (Herencia.Api, que SI conoce IFormFile) es quien abre el
// Stream y extrae el nombre original antes de llamar al servicio de
// Business (ver CertificadosDefuncionController.Subir).
public interface IAlmacenamientoArchivosService
{
    // Guarda "contenido" de forma permanente y devuelve la RUTA/CLAVE con la
    // que quedo guardado (nunca el nombre original: ver
    // AlmacenamientoLocalService para el motivo).
    Task<string> GuardarArchivoAsync(Stream contenido, string nombreArchivoOriginal);
}
