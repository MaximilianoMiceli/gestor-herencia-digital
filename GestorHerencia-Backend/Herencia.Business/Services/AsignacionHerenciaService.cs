using System.Security.Cryptography;
using Herencia.Business.Dtos;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Herencia.Data.Models;
using Herencia.Data.Repositories;

namespace Herencia.Business.Services;

// AsignacionHerenciaService es la implementacion CONCRETA de
// IAsignacionHerenciaService. Depende de TRES repositorios (AsignacionHerencia,
// ActivoDigital y Usuario) porque sus reglas de negocio involucran a las tres
// entidades: para repartir un activo entre beneficiarios hay que conocer el
// activo, resolver (por email) si el beneficiario ya tiene cuenta de Usuario,
// y persistir filas en la tabla intermedia. Notar que ya NO depende de
// IBeneficiarioRepository (esa entidad dejo de existir): donde antes se
// buscaba un Beneficiario por Id, ahora se busca un Usuario por Email.
public class AsignacionHerenciaService : IAsignacionHerenciaService
{
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

            // El UsuarioOtorganteId de cada DTO se completa con el titular
            // del PROPIO activo (activoDigital.UsuarioId), ya conocido en
            // este punto: no hace falta el Include(a => a.ActivoDigital) de
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
    // Id del otorgante (via Include del ActivoDigital relacionado).
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

    // ObtenerAsignacionPorTokenAsync: variante publica de
    // ObtenerAsignacionPorIdAsync, que busca por TokenInvitacion (el
    // identificador no adivinable) en vez de por el Id entero interno. La
    // usa InvitacionesController, que expone este dato sin JWT.
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

    // ObtenerAsignacionesPorUsuarioBeneficiarioAsync: "mis herencias
    // recibidas" del Usuario autenticado. A diferencia de
    // ObtenerAsignacionesPorActivoAsync, aca SI necesitamos el Include del
    // ActivoDigital para cada fila (cada asignacion puede pertenecer a un
    // activo/otorgante distinto), por eso se usa el repositorio dedicado en
    // vez de mapear "a mano".
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

    // CrearAsignacionesAsync: el corazon de la operacion TRANSACCIONAL
    // maestro-detalle. Reparte un ActivoDigital entre uno o varios
    // beneficiarios (invitados por Email) en un solo paso atomico.
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
            // RepositorioBase.EjecutarEnTransaccionAsync). Esto garantiza que
            // en un lote de varias asignaciones, si UNA resulta invalida, las
            // anteriores -ya insertadas con exito dentro de ESTA misma
            // transaccion- se deshacen tambien: no queda ningun rastro
            // parcial en la base de datos.
            await _asignacionHerenciaRepository.EjecutarEnTransaccionAsync(async () =>
            {
                foreach (var itemDto in lote)
                {
                    var emailNormalizado = itemDto.EmailBeneficiario.Trim();

                    // --- Resolucion del beneficiario POR EMAIL ---
                    // A diferencia del modelo anterior (donde el beneficiario
                    // se elegia por un BeneficiarioId ya existente y
                    // validado), aca el otorgante simplemente escribe un
                    // Email: puede o no corresponder ya a una cuenta.
                    var usuarioBeneficiario = await _usuarioRepository.ObtenerPorEmailAsync(emailNormalizado);

                    // --- Validacion: no se puede uno auto-asignarse su propio activo ---
                    // Ademas de no tener sentido de negocio ("heredarse a uno
                    // mismo"), esta regla evita el UNICO escenario en el que
                    // las dos rutas de cascada configuradas en AppDbContext
                    // (Usuario -> ActivoDigital -> AsignacionHerencia, y
                    // Usuario -> AsignacionHerencia directo) podrian converger
                    // sobre la MISMA fila si el otorgante y el beneficiario
                    // fueran la misma persona (ver el comentario detallado en
                    // AppDbContext.OnModelCreating).
                    if (usuarioBeneficiario is not null && usuarioBeneficiario.Id == activoDigital.UsuarioId)
                    {
                        throw new ReglaNegocioException(
                            "El otorgante no puede asignarse un activo digital a si mismo.");
                    }

                    // --- Validacion: el porcentaje ACUMULADO no puede superar el 100% ---
                    porcentajeAcumulado += itemDto.PorcentajeAsignado;

                    if (porcentajeAcumulado > 100)
                    {
                        throw new ReglaNegocioException(
                            $"La suma de porcentajes asignados para este activo supera el 100% (acumulado: {porcentajeAcumulado}%).");
                    }

                    var asignacion = new AsignacionHerencia
                    {
                        ActivoDigitalId = activoDigitalId,
                        // Si ya existe una cuenta con este email, se vincula
                        // de una (UsuarioId completo); si no, queda en null
                        // hasta que esa persona se registre (ver
                        // UsuarioService.CrearUsuarioAsync).
                        UsuarioId = usuarioBeneficiario?.Id,
                        EmailInvitado = emailNormalizado,
                        PorcentajeAsignado = itemDto.PorcentajeAsignado,
                        CondicionLiberacion = itemDto.CondicionLiberacion?.Trim() ?? string.Empty,
                        // TokenInvitacion: identificador PUBLICO no
                        // adivinable de esta fila (ver el comentario
                        // detallado en AsignacionHerencia.cs). Se genera
                        // ACA, una unica vez, con un generador
                        // criptograficamente seguro (32 bytes = 256 bits de
                        // entropia expresados como 64 caracteres hex).
                        TokenInvitacion = RandomNumberGenerator.GetHexString(64),
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

    // CambiarEstadoAsync: variante AUTENTICADA (busca por Id entero interno)
    // del cambio de estado. La usa AsignacionesController.CambiarEstado,
    // DESPUES de verificar ownership por Token JWT.
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

    // CambiarEstadoPorTokenAsync: variante PUBLICA (busca por
    // TokenInvitacion, el identificador no adivinable) del cambio de
    // estado. La usa InvitacionesController.ProcesarInvitacion, que no
    // exige login (ver el comentario de CambiarEstadoInternoAsync sobre los
    // dos modelos de confianza).
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

    // CambiarEstadoInternoAsync: nucleo COMPARTIDO por las dos variantes de
    // arriba (por Id autenticado, y por Token publico). Mueve una
    // AsignacionHerencia de "Pendiente" hacia un estado FINAL ("Aceptado" o
    // "Rechazado"), protegiendo la integridad del proceso de aceptacion.
    private async Task<AsignacionHerenciaDTO> CambiarEstadoInternoAsync(AsignacionHerencia asignacion, EstadoBeneficiario nuevoEstado)
    {
        // Validacion 0: "nuevoEstado" tiene que ser una decision real
        // (Aceptado o Rechazado), nunca "Pendiente". "Pendiente" es un
        // estado que SOLO el sistema asigna automaticamente al crear la
        // asignacion; permitir que alguien lo vuelva a fijar manualmente
        // equivaldria a "deshacer" una decision que todavia no se tomo.
        if (nuevoEstado == EstadoBeneficiario.Pendiente)
        {
            throw new ReglaNegocioException(
                "No se puede establecer el estado en Pendiente manualmente: ese es el estado inicial que el sistema asigna automaticamente.");
        }

        try
        {
            // Notar que NO se exige aca que asignacion.UsuarioId ya este
            // completo (la invitacion podria seguir sin reclamar por
            // ninguna cuenta): este metodo es compartido por DOS puntos de
            // entrada con modelos de confianza distintos.
            //  - AsignacionesController.CambiarEstado (autenticado, busca
            //    por Id): el OWNERSHIP se resuelve ahi, comparando el
            //    UsuarioId del Token JWT contra asignacion.UsuarioId; si
            //    todavia es null, esa comparacion nunca coincide y el
            //    controller ya devuelve 403 antes de llegar aca.
            //  - InvitacionesController.ProcesarInvitacion (publico, busca
            //    por TokenInvitacion, sin login): confia en quien conoce el
            //    link recibido por Email, igual que un link de confirmacion
            //    tradicional; no requiere que la persona ya tenga cuenta
            //    para poder aceptar o rechazar.
            //
            // --- VALIDACION CRITICA (protege la integridad del proceso) ---
            // Una vez que la asignacion ya fue "Aceptada" o "Rechazada", ese
            // estado es DEFINITIVO: no se permite ninguna transicion
            // posterior, ni siquiera para "corregir" un error (eso evitaria,
            // por ejemplo, que alguien que ya rechazo una herencia la vuelva
            // a aceptar mas tarde con intereses distintos a los que tenia en
            // el momento de la decision original, o que un Aceptado se
            // cambie a Rechazado despues de que el otorgante ya avanzo con
            // la liberacion del activo asumiendo que estaba confirmado).
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

    // MapearADTO centraliza la conversion Entidad -> DTO de salida. Recibe el
    // "usuarioOtorganteId" como parametro aparte (en vez de leerlo siempre de
    // "asignacion.ActivoDigital.UsuarioId") porque no todos los metodos de
    // este servicio cargan esa propiedad de navegacion via Include: en esos
    // casos, el llamador ya conoce el Id del otorgante por otro camino (ej:
    // el activoDigital ya consultado por separado) y evita asi una excepcion
    // de referencia nula.
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
