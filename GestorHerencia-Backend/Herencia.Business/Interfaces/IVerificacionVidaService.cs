using Herencia.Business.Dtos;

namespace Herencia.Business.Interfaces;

// IVerificacionVidaService es el CONTRATO publico de la logica de negocio
// del monitoreo de actividad del titular (configuracion, check-in, y la
// maquina de estados que dispara recordatorios/escalamiento).
public interface IVerificacionVidaService
{
    // Devuelve la configuracion del titular indicado. A diferencia del
    // resto de los "ObtenerXxxPorIdAsync" de este proyecto, NO lanza
    // RecursoNoEncontradoException si el titular todavia nunca configuro el
    // monitoreo: en ese caso devuelve una configuracion con valores por
    // defecto (Activo=false), porque "todavia no configurado" es un estado
    // normal de la pantalla, no un error.
    Task<ConfiguracionVerificacionVidaDTO> ObtenerConfiguracionAsync(int usuarioId);

    // Crea o actualiza la configuracion del titular autenticado. Si
    // "Activo" viene en true, exige que ContactoConfianzaId corresponda a
    // un beneficiario ya ACEPTADO de algun activo de este mismo titular
    // (misma regla que ya valida verificacion-vida.tsx del lado del
    // cliente). Puede lanzar ReglaNegocioException (frecuencia invalida,
    // falta el contacto, el contacto no es un beneficiario aceptado) o
    // RecursoNoEncontradoException (ContactoConfianzaId no corresponde a
    // ningun Usuario existente).
    Task<ConfiguracionVerificacionVidaDTO> GuardarConfiguracionAsync(int usuarioId, ConfiguracionVerificacionVidaActualizacionDTO configuracionDTO);

    // Registra que el titular confirmo actividad "ahora": resetea el
    // vencimiento (UltimoCheckIn = UtcNow), vuelve el Estado a Activo, y si
    // habia algun CertificadoDefuncion Pendiente para este titular, lo
    // cancela automaticamente (Estado = CanceladoPorActividad), dejando el
    // registro. Puede lanzar RecursoNoEncontradoException si el titular
    // todavia no tiene ninguna configuracion creada.
    Task<ConfiguracionVerificacionVidaDTO> RegistrarCheckInAsync(int usuarioId);

    // Recorre TODAS las configuraciones activas y aplica la maquina de
    // estados de vencimiento/recordatorios/escalamiento. Es el metodo que
    // invoca VerificacionVidaBackgroundService en cada tick; no esta
    // pensado para ser llamado desde ningun controller.
    Task EjecutarEscaneoAsync();
}
