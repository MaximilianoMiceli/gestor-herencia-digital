using Herencia.Data.Models;

namespace Herencia.Data.Repositories;

// IActivoDigitalRepository extiende el contrato generico IRepositorioBase<ActivoDigital>
// para sumar consultas propias del dominio de ActivoDigital.
public interface IActivoDigitalRepository : IRepositorioBase<ActivoDigital>
{
    // Devuelve todos los ActivosDigitales que pertenecen a un Usuario puntual.
    // Se usa IEnumerable<ActivoDigital> (una coleccion, no un unico objeto) porque
    // un Usuario puede tener CERO, uno o muchos activos digitales registrados.
    Task<IEnumerable<ActivoDigital>> ObtenerActivosPorUsuarioAsync(int usuarioId);
}
