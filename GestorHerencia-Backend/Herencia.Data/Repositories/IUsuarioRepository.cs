using Herencia.Data.Models;

namespace Herencia.Data.Repositories;

// IUsuarioRepository extiende el contrato generico IRepositorioBase<Usuario> para
// sumar operaciones de consulta ESPECIFICAS del dominio de Usuario, que no tendria
// sentido meter dentro del repositorio generico (porque no aplican a cualquier T).
public interface IUsuarioRepository : IRepositorioBase<Usuario>
{
    // Obtiene un Usuario junto con la lista completa de sus Beneficiarios ya
    // cargada, en una unica consulta a la base de datos (Eager Loading).
    // Devuelve "Usuario?" porque el usuarioId solicitado podria no existir.
    Task<Usuario?> ObtenerConBeneficiariosAsync(int usuarioId);
}
