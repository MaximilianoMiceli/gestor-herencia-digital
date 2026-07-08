using Herencia.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Herencia.Data.Repositories;

// BeneficiarioRepository hereda el CRUD generico de RepositorioBase<Beneficiario>
// y suma la consulta especifica ObtenerBeneficiariosPorUsuarioAsync, siguiendo el
// mismo esquema que ActivoDigitalRepository/UsuarioRepository.
public class BeneficiarioRepository : RepositorioBase<Beneficiario>, IBeneficiarioRepository
{
    public BeneficiarioRepository(AppDbContext contexto) : base(contexto)
    {
    }

    // ObtenerBeneficiariosPorUsuarioAsync: filtra los Beneficiarios que
    // pertenecen a un Usuario especifico.
    public async Task<IEnumerable<Beneficiario>> ObtenerBeneficiariosPorUsuarioAsync(int usuarioId)
    {
        // Mismo patron que ActivoDigitalRepository.ObtenerActivosPorUsuarioAsync:
        // el filtro por UsuarioId se traduce a un "WHERE" en SQL, ejecutado del
        // lado de la base de datos, no en memoria.
        return await _contexto.Beneficiarios
            .Where(b => b.UsuarioId == usuarioId)
            .ToListAsync();
    }
}
