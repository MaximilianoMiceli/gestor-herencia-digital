using Herencia.Business.Dtos;

namespace Herencia.Business.Interfaces;

/// <summary>
/// Contrato de la logica de negocio del monitoreo de actividad del titular:
/// configuracion, check-in, y la maquina de estados que dispara recordatorios/escalamiento.
/// </summary>
public interface IVerificacionVidaService
{
    /// <summary>
    /// Devuelve la configuracion del titular; a diferencia de otros "ObtenerXxxPorIdAsync",
    /// no lanza RecursoNoEncontradoException, devuelve una config por defecto (Activo=false).
    /// </summary>
    Task<ConfiguracionVerificacionVidaDTO> ObtenerConfiguracionAsync(int usuarioId);

    /// <summary>
    /// Crea o actualiza la configuracion; si "Activo" es true, exige un ContactoConfianzaId
    /// que sea beneficiario ya aceptado de algun activo del titular.
    /// </summary>
    /// <exception cref="ReglaNegocioException">Frecuencia invalida, falta el contacto, o el contacto no es un beneficiario aceptado.</exception>
    /// <exception cref="RecursoNoEncontradoException">ContactoConfianzaId no corresponde a ningun Usuario existente.</exception>
    Task<ConfiguracionVerificacionVidaDTO> GuardarConfiguracionAsync(int usuarioId, ConfiguracionVerificacionVidaActualizacionDTO configuracionDTO);

    /// <summary>
    /// Registra actividad del titular: resetea el vencimiento, vuelve el Estado a Activo,
    /// y cancela automaticamente cualquier CertificadoDefuncion Pendiente.
    /// </summary>
    /// <exception cref="RecursoNoEncontradoException">El titular todavia no tiene ninguna configuracion creada.</exception>
    Task<ConfiguracionVerificacionVidaDTO> RegistrarCheckInAsync(int usuarioId);

    /// <summary>
    /// Aplica la maquina de estados de vencimiento/recordatorios/escalamiento a todas las
    /// configuraciones activas. Invocado por VerificacionVidaBackgroundService en cada tick.
    /// </summary>
    Task EjecutarEscaneoAsync();
}
