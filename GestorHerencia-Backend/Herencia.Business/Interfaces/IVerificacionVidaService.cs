using Herencia.Business.Dtos;

namespace Herencia.Business.Interfaces;

/// <summary>
/// Contrato de la logica de negocio del monitoreo de actividad del titular:
/// configuracion, check-in, y la maquina de estados que dispara recordatorios/escalamiento.
/// </summary>
public interface IVerificacionVidaService
{
    /// <summary>
    /// Devuelve la configuracion del titular indicado.
    /// </summary>
    /// <remarks>
    /// A diferencia del resto de los "ObtenerXxxPorIdAsync" del proyecto, no lanza
    /// RecursoNoEncontradoException si el titular todavia no configuro el monitoreo: en
    /// ese caso devuelve una configuracion por defecto (Activo=false), porque "todavia no
    /// configurado" es un estado normal de la pantalla, no un error.
    /// </remarks>
    Task<ConfiguracionVerificacionVidaDTO> ObtenerConfiguracionAsync(int usuarioId);

    /// <summary>
    /// Crea o actualiza la configuracion del titular autenticado. Si "Activo" es true,
    /// exige que ContactoConfianzaId corresponda a un beneficiario ya aceptado de algun
    /// activo de este mismo titular.
    /// </summary>
    /// <exception cref="ReglaNegocioException">Frecuencia invalida, falta el contacto, o el contacto no es un beneficiario aceptado.</exception>
    /// <exception cref="RecursoNoEncontradoException">ContactoConfianzaId no corresponde a ningun Usuario existente.</exception>
    Task<ConfiguracionVerificacionVidaDTO> GuardarConfiguracionAsync(int usuarioId, ConfiguracionVerificacionVidaActualizacionDTO configuracionDTO);

    /// <summary>
    /// Registra que el titular confirmo actividad ahora: resetea el vencimiento
    /// (UltimoCheckIn = UtcNow), vuelve el Estado a Activo, y si habia algun
    /// CertificadoDefuncion Pendiente para este titular, lo cancela automaticamente
    /// (Estado = CanceladoPorActividad).
    /// </summary>
    /// <exception cref="RecursoNoEncontradoException">El titular todavia no tiene ninguna configuracion creada.</exception>
    Task<ConfiguracionVerificacionVidaDTO> RegistrarCheckInAsync(int usuarioId);

    /// <summary>
    /// Recorre todas las configuraciones activas y aplica la maquina de estados de
    /// vencimiento/recordatorios/escalamiento. Invocado por
    /// VerificacionVidaBackgroundService en cada tick; no esta pensado para un controller.
    /// </summary>
    Task EjecutarEscaneoAsync();
}
