using Herencia.Data.Models;

namespace Herencia.Data.Repositories;

// IAsignacionHerenciaRepository extiende el contrato generico
// IRepositorioBase<AsignacionHerencia> (que ahora incluye
// EjecutarEnTransaccionAsync) para sumar consultas propias de la relacion
// N-N entre ActivoDigital y Beneficiario.
public interface IAsignacionHerenciaRepository : IRepositorioBase<AsignacionHerencia>
{
    // Devuelve todas las AsignacionHerencia de un ActivoDigital puntual (el
    // "detalle" de la relacion maestro-detalle ActivoDigital -> AsignacionHerencia).
    Task<IEnumerable<AsignacionHerencia>> ObtenerPorActivoDigitalAsync(int activoDigitalId);

    // Busca una unica AsignacionHerencia por su Id, trayendo tambien de forma
    // ANSIOSA (Eager Loading) su ActivoDigital relacionado. Se necesita el
    // ActivoDigital (y, a traves de el, su UsuarioId) para poder resolver la
    // verificacion de OWNERSHIP en el controller: "¿el usuario autenticado es
    // el titular del ActivoDigital al que pertenece esta asignacion?".
    Task<AsignacionHerencia?> ObtenerConActivoDigitalAsync(int id);
}
