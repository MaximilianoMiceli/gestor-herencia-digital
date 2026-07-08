using Herencia.Business.Dtos;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Herencia.Data.Models;
using Herencia.Data.Repositories;

namespace Herencia.Business.Services;

// AsignacionHerenciaService es la implementacion CONCRETA de
// IAsignacionHerenciaService. Depende de TRES repositorios (AsignacionHerencia,
// ActivoDigital y Beneficiario) porque sus reglas de negocio involucran a las
// tres entidades: para repartir un activo entre beneficiarios hay que conocer
// el activo, los beneficiarios destino, y persistir filas en la tabla
// intermedia.
public class AsignacionHerenciaService : IAsignacionHerenciaService
{
    private readonly IAsignacionHerenciaRepository _asignacionHerenciaRepository;
    private readonly IActivoDigitalRepository _activoDigitalRepository;
    private readonly IBeneficiarioRepository _beneficiarioRepository;

    public AsignacionHerenciaService(
        IAsignacionHerenciaRepository asignacionHerenciaRepository,
        IActivoDigitalRepository activoDigitalRepository,
        IBeneficiarioRepository beneficiarioRepository)
    {
        _asignacionHerenciaRepository = asignacionHerenciaRepository;
        _activoDigitalRepository = activoDigitalRepository;
        _beneficiarioRepository = beneficiarioRepository;
    }

    // ObtenerAsignacionesPorActivoAsync: lista el detalle de un ActivoDigital.
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

            // El UsuarioId de cada DTO se completa con el titular del PROPIO
            // activo (activoDigital.UsuarioId), ya conocido en este punto: no
            // hace falta el Include(a => a.ActivoDigital) de
            // ObtenerConActivoDigitalAsync para este caso particular, porque
            // TODAS las asignaciones devueltas por ObtenerPorActivoDigitalAsync
            // pertenecen, por definicion, al mismo activoDigitalId ya validado.
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

    // ObtenerAsignacionPorIdAsync: busca una unica asignacion, incluyendo el
    // UsuarioId de su titular (via Include del ActivoDigital relacionado).
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

    // CrearAsignacionesAsync: el corazon de la operacion TRANSACCIONAL
    // maestro-detalle. Reparte un ActivoDigital entre uno o varios
    // Beneficiarios en un solo paso atomico.
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
            // --- Paso 1: validar que el activo exista ANTES de abrir la transaccion ---
            // No tiene sentido ni siquiera abrir una transaccion de base de
            // datos si el activo titular ya no existe.
            var activoDigital = await _activoDigitalRepository.ObtenerPorIdAsync(activoDigitalId);

            if (activoDigital is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el activo digital con Id {activoDigitalId}.");
            }

            // Se parte del porcentaje YA comprometido por asignaciones
            // PREVIAS de este mismo activo (si las hubiera), para validar el
            // acumulado TOTAL (viejas + nuevas) contra el limite de 100%.
            var asignacionesExistentes = await _asignacionHerenciaRepository.ObtenerPorActivoDigitalAsync(activoDigitalId);
            var porcentajeAcumulado = asignacionesExistentes.Sum(a => a.PorcentajeAsignado);

            var asignacionesCreadas = new List<AsignacionHerencia>();

            // --- Paso 2: TODO el lote se procesa dentro de UNA transaccion ---
            // EjecutarEnTransaccionAsync abre una transaccion real de base de
            // datos ANTES de ejecutar el lambda, y la revierte POR COMPLETO
            // si el lambda lanza cualquier excepcion en cualquier punto (ver
            // RepositorioBase.EjecutarEnTransaccionAsync). Esto es lo que
            // garantiza la regla pedida por la rubrica: "si ocurre un error
            // dentro del proceso, la operacion no debe guardar informacion
            // parcial". Por ejemplo, en un lote de 5 asignaciones, si la
            // NUMERO 3 resulta invalida (beneficiario inexistente, o hace
            // superar el 100%), las 2 asignaciones anteriores -que ya se
            // habian insertado con exito dentro de ESTA misma transaccion- se
            // deshacen tambien: no queda ningun rastro parcial en la base de
            // datos.
            await _asignacionHerenciaRepository.EjecutarEnTransaccionAsync(async () =>
            {
                foreach (var itemDto in lote)
                {
                    // --- Validacion 1: el beneficiario debe existir ---
                    var beneficiario = await _beneficiarioRepository.ObtenerPorIdAsync(itemDto.BeneficiarioId);

                    if (beneficiario is null)
                    {
                        throw new RecursoNoEncontradoException(
                            $"No se encontro el beneficiario con Id {itemDto.BeneficiarioId}.");
                    }

                    // --- Validacion 2: el beneficiario debe pertenecer al MISMO titular que el activo ---
                    // Regla de negocio explicita: no tiene sentido (ni es
                    // seguro) repartir el activo de un Usuario hacia un
                    // Beneficiario que registro OTRO Usuario distinto.
                    if (beneficiario.UsuarioId != activoDigital.UsuarioId)
                    {
                        throw new ReglaNegocioException(
                            $"El beneficiario con Id {itemDto.BeneficiarioId} no pertenece al mismo titular que el activo digital.");
                    }

                    // --- Validacion 3: el porcentaje ACUMULADO no puede superar el 100% ---
                    // Se sigue sumando sobre la MISMA variable a medida que
                    // se procesa cada item del lote: si el item actual hace
                    // que el acumulado supere 100, se corta ACA, con todo lo
                    // insertado hasta el momento (dentro de esta transaccion)
                    // pendiente de ser revertido por el catch de
                    // EjecutarEnTransaccionAsync.
                    porcentajeAcumulado += itemDto.PorcentajeAsignado;

                    if (porcentajeAcumulado > 100)
                    {
                        throw new ReglaNegocioException(
                            $"La suma de porcentajes asignados para este activo supera el 100% (acumulado: {porcentajeAcumulado}%).");
                    }

                    var asignacion = new AsignacionHerencia
                    {
                        ActivoDigitalId = activoDigitalId,
                        BeneficiarioId = itemDto.BeneficiarioId,
                        PorcentajeAsignado = itemDto.PorcentajeAsignado,
                        CondicionLiberacion = itemDto.CondicionLiberacion?.Trim() ?? string.Empty,
                        FechaCreacion = DateTime.UtcNow,
                        UsuarioCreacion = "sistema"
                    };

                    // AgregarAsync ya ejecuta su propio SaveChangesAsync
                    // internamente (ver RepositorioBase), pero como estamos
                    // dentro de la transaccion explicita abierta por
                    // EjecutarEnTransaccionAsync, ese SaveChangesAsync NO se
                    // confirma de forma independiente: queda "en espera"
                    // hasta el Commit final (o se descarta entero si hay
                    // Rollback).
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

    // ActualizarAsignacionAsync: modifica Porcentaje/Condicion de UNA
    // asignacion existente.
    public async Task<AsignacionHerenciaDTO> ActualizarAsignacionAsync(int id, AsignacionHerenciaActualizacionDTO asignacionActualizacionDTO)
    {
        try
        {
            var asignacion = await _asignacionHerenciaRepository.ObtenerConActivoDigitalAsync(id);

            if (asignacion is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro la asignacion de herencia con Id {id}.");
            }

            // Se recalcula el acumulado de las DEMAS asignaciones del mismo
            // activo (excluyendo esta misma, que se va a reemplazar) para
            // validar que el NUEVO porcentaje siga sin superar el 100% total.
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

    // EliminarAsignacionAsync: borra una asignacion existente por su Id.
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

    // MapearADTO centraliza la conversion Entidad -> DTO de salida. Recibe el
    // "usuarioId" del titular como parametro aparte (en vez de leerlo de
    // "asignacion.ActivoDigital.UsuarioId" siempre) porque no todos los
    // metodos de este servicio cargan esa propiedad de navegacion via
    // Include: en esos casos, el llamador ya conoce el UsuarioId por otro
    // camino (ej: el activoDigital ya consultado por separado) y evita asi
    // una excepcion de referencia nula.
    private static AsignacionHerenciaDTO MapearADTO(AsignacionHerencia asignacion, int usuarioId)
    {
        return new AsignacionHerenciaDTO
        {
            Id = asignacion.Id,
            ActivoDigitalId = asignacion.ActivoDigitalId,
            BeneficiarioId = asignacion.BeneficiarioId,
            PorcentajeAsignado = asignacion.PorcentajeAsignado,
            CondicionLiberacion = asignacion.CondicionLiberacion,
            UsuarioId = usuarioId
        };
    }
}
