using System.Security.Cryptography;
using Herencia.Business.Dtos;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Herencia.Data.Models;
using Herencia.Data.Repositories;

namespace Herencia.Business.Services;

/// <summary>
/// Implementación de <see cref="IAsignacionHerenciaService"/>: reparte activos digitales
/// entre beneficiarios (invitados por email) y gestiona el ciclo de vida de cada invitación.
/// </summary>
public class AsignacionHerenciaService : IAsignacionHerenciaService
{
    // Depende de los tres repositorios porque sus reglas de negocio involucran a las tres
    // entidades: para repartir un activo hay que conocerlo, resolver por email si el
    // beneficiario ya tiene cuenta, y persistir la asignación.
    private readonly IAsignacionHerenciaRepository _asignacionHerenciaRepository;
    private readonly IActivoDigitalRepository _activoDigitalRepository;
    private readonly IUsuarioRepository _usuarioRepository;

    public AsignacionHerenciaService(
        IAsignacionHerenciaRepository asignacionHerenciaRepository,
        IActivoDigitalRepository activoDigitalRepository,
        IUsuarioRepository usuarioRepository)
    {
        _asignacionHerenciaRepository = asignacionHerenciaRepository;
        _activoDigitalRepository = activoDigitalRepository;
        _usuarioRepository = usuarioRepository;
    }

    /// <summary>
    /// Lista las asignaciones de un activo digital.
    /// </summary>
    public async Task<IEnumerable<AsignacionHerenciaDTO>> ObtenerAsignacionesPorActivoAsync(int activoDigitalId)
    {
        try
        {
            var activoDigital = await _activoDigitalRepository.ObtenerPorIdAsync(activoDigitalId);

            if (activoDigital is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el activo digital con Id {activoDigitalId}.");
            }

            var asignaciones = await _asignacionHerenciaRepository.ObtenerPorActivoDigitalAsync(activoDigitalId);

            return asignaciones.Select(a => MapearADTO(a, activoDigital.UsuarioId));
        }
        catch (RecursoNoEncontradoException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al obtener las asignaciones del activo digital.", ex);
        }
    }

    /// <summary>
    /// Busca una asignación por Id, incluyendo el otorgante (vía el activo digital relacionado).
    /// </summary>
    public async Task<AsignacionHerenciaDTO> ObtenerAsignacionPorIdAsync(int id)
    {
        try
        {
            var asignacion = await _asignacionHerenciaRepository.ObtenerConActivoDigitalAsync(id);

            if (asignacion is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro la asignacion de herencia con Id {id}.");
            }

            return MapearADTO(asignacion, asignacion.ActivoDigital.UsuarioId);
        }
        catch (RecursoNoEncontradoException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al obtener la asignacion de herencia.", ex);
        }
    }

    /// <summary>
    /// Busca una invitación por su token (identificador público no adivinable), usado por
    /// endpoints que no requieren autenticación JWT.
    /// </summary>
    public async Task<AsignacionHerenciaDTO> ObtenerAsignacionPorTokenAsync(string token)
    {
        try
        {
            var asignacion = await _asignacionHerenciaRepository.ObtenerPorTokenInvitacionAsync(token);

            if (asignacion is null)
            {
                throw new RecursoNoEncontradoException("No se encontro ninguna invitacion con ese token.");
            }

            return MapearADTO(asignacion, asignacion.ActivoDigital.UsuarioId);
        }
        catch (RecursoNoEncontradoException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al obtener la invitacion.", ex);
        }
    }

    /// <summary>
    /// Devuelve las herencias recibidas por un usuario beneficiario.
    /// </summary>
    public async Task<IEnumerable<AsignacionHerenciaDTO>> ObtenerAsignacionesPorUsuarioBeneficiarioAsync(int usuarioId)
    {
        try
        {
            var asignaciones = await _asignacionHerenciaRepository.ObtenerPorUsuarioBeneficiarioAsync(usuarioId);

            return asignaciones.Select(a => MapearADTO(a, a.ActivoDigital.UsuarioId));
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al obtener las herencias recibidas del usuario.", ex);
        }
    }

    /// <summary>
    /// Reparte un activo digital entre uno o varios beneficiarios (invitados por email)
    /// en una única operación transaccional.
    /// </summary>
    public async Task<IEnumerable<AsignacionHerenciaDTO>> CrearAsignacionesAsync(
        int activoDigitalId,
        IEnumerable<AsignacionHerenciaCreacionDTO> asignacionesCreacionDTO)
    {
        var lote = asignacionesCreacionDTO.ToList();

        if (lote.Count == 0)
        {
            throw new ReglaNegocioException("Debe enviar al menos una asignacion para procesar.");
        }

        try
        {
            var activoDigital = await _activoDigitalRepository.ObtenerPorIdAsync(activoDigitalId);

            if (activoDigital is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el activo digital con Id {activoDigitalId}.");
            }

            // Se parte del porcentaje ya comprometido por asignaciones previas para validar
            // el acumulado total (viejas + nuevas) contra el límite de 100%.
            var asignacionesExistentes = await _asignacionHerenciaRepository.ObtenerPorActivoDigitalAsync(activoDigitalId);
            var porcentajeAcumulado = asignacionesExistentes.Sum(a => a.PorcentajeAsignado);

            var asignacionesCreadas = new List<AsignacionHerencia>();

            // Todo el lote se procesa dentro de una única transacción: si una asignación
            // resulta inválida, las anteriores ya insertadas en este mismo lote se
            // revierten también, sin dejar rastro parcial en la base de datos.
            await _asignacionHerenciaRepository.EjecutarEnTransaccionAsync(async () =>
            {
                foreach (var itemDto in lote)
                {
                    var emailNormalizado = itemDto.EmailBeneficiario.Trim();

                    // El beneficiario se resuelve por email: puede o no corresponder ya a una cuenta.
                    var usuarioBeneficiario = await _usuarioRepository.ObtenerPorEmailAsync(emailNormalizado);

                    // No se permite auto-asignación: además de no tener sentido de negocio,
                    // evita que las dos rutas de cascada de AppDbContext (Usuario ->
                    // ActivoDigital -> AsignacionHerencia, y Usuario -> AsignacionHerencia)
                    // converjan sobre la misma fila si otorgante y beneficiario coincidieran.
                    if (usuarioBeneficiario is not null && usuarioBeneficiario.Id == activoDigital.UsuarioId)
                    {
                        throw new ReglaNegocioException(
                            "El otorgante no puede asignarse un activo digital a si mismo.");
                    }

                    porcentajeAcumulado += itemDto.PorcentajeAsignado;

                    if (porcentajeAcumulado > 100)
                    {
                        throw new ReglaNegocioException(
                            $"La suma de porcentajes asignados para este activo supera el 100% (acumulado: {porcentajeAcumulado}%).");
                    }

                    var asignacion = new AsignacionHerencia
                    {
                        ActivoDigitalId = activoDigitalId,
                        // Si ya existe una cuenta con este email se vincula de una; si no,
                        // queda en null hasta que esa persona se registre.
                        UsuarioId = usuarioBeneficiario?.Id,
                        EmailInvitado = emailNormalizado,
                        PorcentajeAsignado = itemDto.PorcentajeAsignado,
                        CondicionLiberacion = itemDto.CondicionLiberacion?.Trim() ?? string.Empty,
                        // Identificador público no adivinable de la invitación (256 bits de entropía).
                        TokenInvitacion = RandomNumberGenerator.GetHexString(64),
                        FechaCreacion = DateTime.UtcNow,
                        UsuarioCreacion = "sistema"
                    };

                    await _asignacionHerenciaRepository.AgregarAsync(asignacion);
                    asignacionesCreadas.Add(asignacion);
                }
            });

            return asignacionesCreadas.Select(a => MapearADTO(a, activoDigital.UsuarioId));
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
            throw new ReglaNegocioException("Ocurrio un error al procesar las asignaciones de herencia.", ex);
        }
    }

    /// <summary>
    /// Modifica el porcentaje y la condición de liberación de una asignación existente.
    /// </summary>
    public async Task<AsignacionHerenciaDTO> ActualizarAsignacionAsync(int id, AsignacionHerenciaActualizacionDTO asignacionActualizacionDTO)
    {
        try
        {
            var asignacion = await _asignacionHerenciaRepository.ObtenerConActivoDigitalAsync(id);

            if (asignacion is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro la asignacion de herencia con Id {id}.");
            }

            // Se recalcula el acumulado de las demás asignaciones del mismo activo
            // (excluyendo esta) para validar que el nuevo porcentaje no supere el 100% total.
            var otrasAsignaciones = await _asignacionHerenciaRepository.ObtenerPorActivoDigitalAsync(asignacion.ActivoDigitalId);
            var porcentajeAcumuladoSinEsta = otrasAsignaciones
                .Where(a => a.Id != id)
                .Sum(a => a.PorcentajeAsignado);

            if (porcentajeAcumuladoSinEsta + asignacionActualizacionDTO.PorcentajeAsignado > 100)
            {
                throw new ReglaNegocioException(
                    $"La suma de porcentajes asignados para este activo superaria el 100% " +
                    $"(otras asignaciones: {porcentajeAcumuladoSinEsta}%, nuevo valor: {asignacionActualizacionDTO.PorcentajeAsignado}%).");
            }

            asignacion.PorcentajeAsignado = asignacionActualizacionDTO.PorcentajeAsignado;
            asignacion.CondicionLiberacion = asignacionActualizacionDTO.CondicionLiberacion?.Trim() ?? string.Empty;
            asignacion.FechaModificacion = DateTime.UtcNow;
            asignacion.UsuarioModificacion = "sistema";

            await _asignacionHerenciaRepository.ActualizarAsync(asignacion);

            return MapearADTO(asignacion, asignacion.ActivoDigital.UsuarioId);
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
            throw new ReglaNegocioException("Ocurrio un error al actualizar la asignacion de herencia.", ex);
        }
    }

    /// <summary>
    /// Elimina una asignación existente por su Id.
    /// </summary>
    public async Task EliminarAsignacionAsync(int id)
    {
        try
        {
            var asignacion = await _asignacionHerenciaRepository.ObtenerPorIdAsync(id);

            if (asignacion is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro la asignacion de herencia con Id {id}.");
            }

            await _asignacionHerenciaRepository.EliminarAsync(id);
        }
        catch (RecursoNoEncontradoException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al eliminar la asignacion de herencia.", ex);
        }
    }

    /// <summary>
    /// Cambia el estado de una asignación (variante autenticada, busca por Id). El
    /// ownership ya fue verificado por el llamador vía el token JWT.
    /// </summary>
    public async Task<AsignacionHerenciaDTO> CambiarEstadoAsync(int asignacionId, EstadoBeneficiario nuevoEstado)
    {
        AsignacionHerencia? asignacion;

        try
        {
            asignacion = await _asignacionHerenciaRepository.ObtenerConActivoDigitalAsync(asignacionId);
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al buscar la asignacion de herencia.", ex);
        }

        if (asignacion is null)
        {
            throw new RecursoNoEncontradoException($"No se encontro la asignacion de herencia con Id {asignacionId}.");
        }

        return await CambiarEstadoInternoAsync(asignacion, nuevoEstado);
    }

    /// <summary>
    /// Cambia el estado de una invitación (variante pública, busca por token de
    /// invitación). No requiere login: confía en quien conoce el link recibido por email.
    /// </summary>
    public async Task<AsignacionHerenciaDTO> CambiarEstadoPorTokenAsync(string token, EstadoBeneficiario nuevoEstado)
    {
        AsignacionHerencia? asignacion;

        try
        {
            asignacion = await _asignacionHerenciaRepository.ObtenerPorTokenInvitacionAsync(token);
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al buscar la invitacion.", ex);
        }

        if (asignacion is null)
        {
            throw new RecursoNoEncontradoException("No se encontro ninguna invitacion con ese token.");
        }

        return await CambiarEstadoInternoAsync(asignacion, nuevoEstado);
    }

    // Núcleo compartido por las dos variantes de cambio de estado: mueve una asignación
    // de "Pendiente" hacia un estado final ("Aceptado" o "Rechazado").
    private async Task<AsignacionHerenciaDTO> CambiarEstadoInternoAsync(AsignacionHerencia asignacion, EstadoBeneficiario nuevoEstado)
    {
        // "Pendiente" es un estado que solo el sistema asigna al crear la asignación;
        // permitir fijarlo manualmente equivaldría a "deshacer" una decisión no tomada aún.
        if (nuevoEstado == EstadoBeneficiario.Pendiente)
        {
            throw new ReglaNegocioException(
                "No se puede establecer el estado en Pendiente manualmente: ese es el estado inicial que el sistema asigna automaticamente.");
        }

        try
        {
            // Una vez "Aceptada" o "Rechazada", el estado es definitivo: no se permite
            // ninguna transición posterior, ni para "corregir" un error, evitando que
            // alguien revierta una decisión después de que el otorgante ya avanzó
            // asumiendo que estaba confirmada.
            if (asignacion.Estado != EstadoBeneficiario.Pendiente)
            {
                throw new ReglaNegocioException(
                    "El estado ya fue procesado y no puede modificarse.");
            }

            asignacion.Estado = nuevoEstado;
            asignacion.FechaModificacion = DateTime.UtcNow;
            asignacion.UsuarioModificacion = "sistema";

            await _asignacionHerenciaRepository.ActualizarAsync(asignacion);

            return MapearADTO(asignacion, asignacion.ActivoDigital.UsuarioId);
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
            throw new ReglaNegocioException("Ocurrio un error al cambiar el estado de la asignacion de herencia.", ex);
        }
    }

    // Recibe "usuarioOtorganteId" aparte porque no todos los métodos cargan la propiedad
    // de navegación ActivoDigital vía Include; en esos casos el llamador ya conoce ese Id.
    private static AsignacionHerenciaDTO MapearADTO(AsignacionHerencia asignacion, int usuarioOtorganteId)
    {
        return new AsignacionHerenciaDTO
        {
            Id = asignacion.Id,
            ActivoDigitalId = asignacion.ActivoDigitalId,
            UsuarioBeneficiarioId = asignacion.UsuarioId,
            EmailInvitado = asignacion.EmailInvitado,
            PorcentajeAsignado = asignacion.PorcentajeAsignado,
            CondicionLiberacion = asignacion.CondicionLiberacion,
            Estado = asignacion.Estado,
            UsuarioOtorganteId = usuarioOtorganteId,
            TokenInvitacion = asignacion.TokenInvitacion,
            FechaLiberacion = asignacion.FechaLiberacion
        };
    }
}
