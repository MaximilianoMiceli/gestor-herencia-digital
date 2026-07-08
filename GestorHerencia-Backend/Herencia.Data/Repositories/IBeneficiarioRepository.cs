using Herencia.Data.Models;

namespace Herencia.Data.Repositories;

// IBeneficiarioRepository extiende el contrato generico IRepositorioBase<Beneficiario>
// para sumar consultas propias del dominio de Beneficiario, siguiendo el mismo
// criterio ya usado por IUsuarioRepository e IActivoDigitalRepository.
public interface IBeneficiarioRepository : IRepositorioBase<Beneficiario>
{
    // Devuelve todos los Beneficiarios registrados por un Usuario puntual.
    // Un Usuario puede tener CERO, uno o muchos beneficiarios (relacion 1-N).
    Task<IEnumerable<Beneficiario>> ObtenerBeneficiariosPorUsuarioAsync(int usuarioId);
}
