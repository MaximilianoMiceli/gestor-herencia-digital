using Herencia.Data.Models;

namespace Herencia.Data.Repositories;

// IConfiguracionVerificacionVidaRepository extiende el contrato generico
// IRepositorioBase<ConfiguracionVerificacionVida> para sumar las consultas
// propias de este dominio. Notar que "ObtenerPorIdAsync" (heredado) NO
// aplica aca en el sentido tradicional: la PK de esta tabla es UsuarioId
// (clave compartida, ver el comentario de la entidad), asi que
// "ObtenerPorIdAsync(usuarioId)" YA es, en la practica, "buscar por
// usuario". Se agrega igual un metodo con nombre explicito
// (ObtenerPorUsuarioIdAsync) por legibilidad del lado de Business.
public interface IConfiguracionVerificacionVidaRepository : IRepositorioBase<ConfiguracionVerificacionVida>
{
    // Busca la configuracion de UN titular puntual. Devuelve null si ese
    // Usuario todavia no configuro nunca el monitoreo.
    Task<ConfiguracionVerificacionVida?> ObtenerPorUsuarioIdAsync(int usuarioId);

    // Devuelve TODAS las configuraciones con Activo == true: es la
    // consulta que usa VerificacionVidaBackgroundService en cada tick para
    // saber sobre que titulares tiene que evaluar vencimientos.
    Task<IEnumerable<ConfiguracionVerificacionVida>> ObtenerActivasParaEscaneoAsync();
}
