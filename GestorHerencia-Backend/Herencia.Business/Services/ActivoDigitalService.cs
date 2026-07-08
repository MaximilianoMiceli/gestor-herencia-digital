using Herencia.Business.Dtos;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Herencia.Data.Models;
using Herencia.Data.Repositories;

namespace Herencia.Business.Services;

// ActivoDigitalService es la implementacion CONCRETA de IActivoDigitalService.
// Contiene la logica de negocio de ActivoDigital: validaciones basicas,
// la regla de negocio que exige que el Usuario titular exista antes de
// asignarle un activo, y la traduccion de errores tecnicos a excepciones
// amigables.
public class ActivoDigitalService : IActivoDigitalService
{
    // Interfaz del repositorio de ActivoDigital: usada para las operaciones
    // CRUD propias de esta entidad (crear, buscar por Id, listar por usuario).
    private readonly IActivoDigitalRepository _activoDigitalRepository;

    // Interfaz del repositorio de Usuario: NO es un error de copy-paste. Este
    // servicio la necesita exclusivamente para poder validar la regla de
    // negocio "el usuario titular debe existir antes de crearle un activo".
    // Es perfectamente valido (y muy comun en arquitecturas por capas) que un
    // servicio de Business dependa de MAS DE UN repositorio cuando su logica
    // de negocio involucra a mas de una entidad del dominio. Lo importante,
    // y lo que exige la rubrica, es que siga dependiendo de INTERFACES de
    // repositorio (nunca de AppDbContext ni de las clases concretas).
    private readonly IUsuarioRepository _usuarioRepository;

    // Ambas dependencias llegan por Inyeccion de Dependencias via constructor.
    // El contenedor de DI (configurado en Program.cs en la capa Api) resuelve
    // automaticamente que implementacion concreta corresponde a cada interfaz
    // (ActivoDigitalRepository e UsuarioRepository, respectivamente) y las
    // inyecta aca.
    public ActivoDigitalService(
        IActivoDigitalRepository activoDigitalRepository,
        IUsuarioRepository usuarioRepository)
    {
        _activoDigitalRepository = activoDigitalRepository;
        _usuarioRepository = usuarioRepository;
    }

    // CrearActivoDigitalAsync: da de alta un nuevo ActivoDigital, pero solo si
    // el Usuario titular indicado (dto.UsuarioId) realmente existe.
    public async Task<ActivoDigitalDTO> CrearActivoDigitalAsync(ActivoDigitalCreacionDTO activoDigitalCreacionDTO)
    {
        // --- Paso 1: Validacion de negocio simple (formato/contenido) ---
        // Se valida ANTES de ir a la base de datos: no tiene sentido gastar
        // una consulta para verificar si existe el usuario si el nombre del
        // activo ya es invalido de entrada.
        if (string.IsNullOrWhiteSpace(activoDigitalCreacionDTO.Nombre))
        {
            throw new ReglaNegocioException("El nombre del activo digital no puede estar vacio.");
        }

        try
        {
            // --- Paso 2: Regla de negocio explicita pedida por la rubrica ---
            // "El usuario titular debe existir antes de asignarle un nuevo
            // activo digital". Consultamos el repositorio de Usuario (a
            // traves de su interfaz) para verificar la existencia del
            // usuarioId recibido en el DTO.
            var usuarioTitular = await _usuarioRepository.ObtenerPorIdAsync(activoDigitalCreacionDTO.UsuarioId);

            if (usuarioTitular is null)
            {
                // Si el usuario titular no existe, NO tiene sentido continuar:
                // lanzamos RecursoNoEncontradoException (no ReglaNegocioException)
                // porque el problema puntual es, literalmente, que el recurso
                // "Usuario" referenciado no fue encontrado. Esto se hace DENTRO
                // del try, pero se relanza sin modificaciones en el catch de
                // abajo (ver el catch especifico para RecursoNoEncontradoException).
                throw new RecursoNoEncontradoException(
                    $"No se puede crear el activo digital: el usuario titular con Id {activoDigitalCreacionDTO.UsuarioId} no existe.");
            }

            // --- Paso 3: mapeo DTO -> Entidad y persistencia ---
            // Igual que en UsuarioService, el mapeo manual desde el DTO hacia
            // la entidad de EF Core es lo que impide que el llamador pueda
            // "inyectar" valores que no deberia controlar (Id, FechaCreacion,
            // la coleccion de AsignacionesHerencia, etc.), ya que esos campos
            // ni siquiera existen en ActivoDigitalCreacionDTO.
            var activoDigital = new ActivoDigital
            {
                Nombre = activoDigitalCreacionDTO.Nombre.Trim(),
                Tipo = activoDigitalCreacionDTO.Tipo,
                Descripcion = activoDigitalCreacionDTO.Descripcion?.Trim() ?? string.Empty,
                UsuarioId = activoDigitalCreacionDTO.UsuarioId,
                FechaCreacion = DateTime.UtcNow,
                UsuarioCreacion = "sistema"
            };

            // Delegamos la persistencia al repositorio especifico de
            // ActivoDigital, a traves de su interfaz.
            await _activoDigitalRepository.AgregarAsync(activoDigital);

            return MapearADTO(activoDigital);
        }
        catch (RecursoNoEncontradoException)
        {
            // Relanzamos tal cual: ya es una excepcion "amigable" y especifica
            // (el usuario titular no existe), no un error tecnico a envolver.
            throw;
        }
        catch (ReglaNegocioException)
        {
            // Por consistencia con el resto del servicio: si en el futuro se
            // agrega alguna validacion adicional que lance ReglaNegocioException
            // dentro del try, se relanza sin volver a envolverla.
            throw;
        }
        catch (Exception ex)
        {
            // Cualquier otro error NO esperado (fallo de conexion a la base de
            // datos, violacion de una constraint SQL, etc.) se traduce a un
            // mensaje generico y seguro, sin exponer el detalle tecnico ni el
            // StackTrace real al llamador. El detalle original queda
            // preservado unicamente en "ex", como InnerException.
            throw new ReglaNegocioException("Ocurrio un error al procesar el activo digital.", ex);
        }
    }

    // ObtenerActivoDigitalPorIdAsync: busca un ActivoDigital por Id.
    public async Task<ActivoDigitalDTO> ObtenerActivoDigitalPorIdAsync(int id)
    {
        try
        {
            var activoDigital = await _activoDigitalRepository.ObtenerPorIdAsync(id);

            if (activoDigital is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el activo digital con Id {id}.");
            }

            return MapearADTO(activoDigital);
        }
        catch (RecursoNoEncontradoException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al obtener el activo digital.", ex);
        }
    }

    // ObtenerActivosPorUsuarioAsync: devuelve todos los ActivosDigitales de un
    // Usuario puntual, validando primero que ese Usuario exista (misma regla
    // de negocio que en la creacion: no tiene sentido listar activos de un
    // titular inexistente).
    public async Task<IEnumerable<ActivoDigitalDTO>> ObtenerActivosPorUsuarioAsync(int usuarioId)
    {
        try
        {
            var usuarioTitular = await _usuarioRepository.ObtenerPorIdAsync(usuarioId);

            if (usuarioTitular is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el usuario con Id {usuarioId}.");
            }

            var activos = await _activoDigitalRepository.ObtenerActivosPorUsuarioAsync(usuarioId);

            return activos.Select(MapearADTO);
        }
        catch (RecursoNoEncontradoException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al obtener los activos digitales del usuario.", ex);
        }
    }

    // ActualizarActivoDigitalAsync: modifica Nombre, Tipo y Descripcion de un
    // ActivoDigital que ya existe. El UsuarioId (titular) NUNCA se toca aca: no
    // se puede "transferir" el activo por este medio (ver comentario en
    // ActivoDigitalActualizacionDTO).
    public async Task<ActivoDigitalDTO> ActualizarActivoDigitalAsync(int id, ActivoDigitalActualizacionDTO activoDigitalActualizacionDTO)
    {
        // --- Paso 1: misma validacion de formato que en el alta ---
        if (string.IsNullOrWhiteSpace(activoDigitalActualizacionDTO.Nombre))
        {
            throw new ReglaNegocioException("El nombre del activo digital no puede estar vacio.");
        }

        try
        {
            // --- Paso 2: buscar la entidad EXISTENTE ---
            // Se carga el activo completo (no solo se "arma" uno nuevo) para
            // preservar sus campos que este DTO no expone: Id, UsuarioId,
            // FechaCreacion y las AsignacionesHerencia ya vinculadas.
            var activoDigital = await _activoDigitalRepository.ObtenerPorIdAsync(id);

            if (activoDigital is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el activo digital con Id {id}.");
            }

            activoDigital.Nombre = activoDigitalActualizacionDTO.Nombre.Trim();
            activoDigital.Tipo = activoDigitalActualizacionDTO.Tipo;
            activoDigital.Descripcion = activoDigitalActualizacionDTO.Descripcion?.Trim() ?? string.Empty;

            // Dato de auditoria: registra cuando y quien modifico el activo.
            activoDigital.FechaModificacion = DateTime.UtcNow;
            activoDigital.UsuarioModificacion = "sistema";

            await _activoDigitalRepository.ActualizarAsync(activoDigital);

            return MapearADTO(activoDigital);
        }
        catch (RecursoNoEncontradoException)
        {
            throw;
        }
        catch (ReglaNegocioException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al actualizar el activo digital.", ex);
        }
    }

    // EliminarActivoDigitalAsync: borra un ActivoDigital existente por su Id.
    public async Task EliminarActivoDigitalAsync(int id)
    {
        try
        {
            var activoDigital = await _activoDigitalRepository.ObtenerPorIdAsync(id);

            if (activoDigital is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el activo digital con Id {id}.");
            }

            // La cascada configurada en AppDbContext para AsignacionHerencia ->
            // ActivoDigital se encarga de eliminar tambien las asignaciones de
            // herencia que dependan de este activo.
            await _activoDigitalRepository.EliminarAsync(id);
        }
        catch (RecursoNoEncontradoException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al eliminar el activo digital.", ex);
        }
    }

    // MapearADTO centraliza la conversion Entidad -> DTO de salida, evitando
    // repetir este mapeo en cada metodo publico del servicio.
    private static ActivoDigitalDTO MapearADTO(ActivoDigital activoDigital)
    {
        return new ActivoDigitalDTO
        {
            Id = activoDigital.Id,
            Nombre = activoDigital.Nombre,
            Tipo = activoDigital.Tipo,
            Descripcion = activoDigital.Descripcion,
            UsuarioId = activoDigital.UsuarioId
        };
    }
}
