using Herencia.Business.Dtos;
using Herencia.Data.Models;

namespace Herencia.Business.Interfaces;

// IAsignacionHerenciaService es el CONTRATO publico de la logica de negocio
// de AsignacionHerencia: la relacion N-N entre ActivoDigital y Usuario (en su
// rol de Beneficiario). Con el modelo de doble rol, este servicio absorbe
// ademas el flujo de aceptacion/rechazo que antes vivia en
// IBeneficiarioService (CambiarEstadoAsync), porque el Estado ahora es un
// atributo de CADA asignacion puntual, no de una entidad Beneficiario aparte.
public interface IAsignacionHerenciaService
{
    // Devuelve todas las asignaciones de un ActivoDigital puntual (el
    // "detalle" de la relacion maestro-detalle ActivoDigital -> AsignacionHerencia).
    // Puede lanzar RecursoNoEncontradoException si el activoDigitalId no existe.
    Task<IEnumerable<AsignacionHerenciaDTO>> ObtenerAsignacionesPorActivoAsync(int activoDigitalId);

    // Busca una unica AsignacionHerencia por su Id (incluye el Id del
    // Usuario otorgante, para que la capa Api pueda resolver la verificacion
    // de ownership).
    // Puede lanzar RecursoNoEncontradoException si el Id no existe.
    Task<AsignacionHerenciaDTO> ObtenerAsignacionPorIdAsync(int id);

    // Variante PUBLICA de ObtenerAsignacionPorIdAsync: busca por
    // TokenInvitacion (el identificador no adivinable expuesto a traves de
    // InvitacionesController) en vez de por el Id entero interno.
    // Puede lanzar RecursoNoEncontradoException si el token no existe.
    Task<AsignacionHerenciaDTO> ObtenerAsignacionPorTokenAsync(string token);

    // Devuelve todas las AsignacionHerencia en las que el Usuario indicado
    // participa como BENEFICIARIO ("mis herencias recibidas").
    Task<IEnumerable<AsignacionHerenciaDTO>> ObtenerAsignacionesPorUsuarioBeneficiarioAsync(int usuarioId);

    // CrearAsignacionesAsync: da de alta un LOTE de asignaciones para un
    // mismo ActivoDigital, TODAS dentro de una unica transaccion atomica
    // (ver RepositorioBase.EjecutarEnTransaccionAsync): si CUALQUIER
    // asignacion del lote es invalida, NINGUNA de las asignaciones del lote
    // queda persistida, ni siquiera las que se hubieran procesado con exito
    // ANTES de la que fallo.
    //
    // Para cada item del lote, se busca un Usuario existente con el
    // EmailBeneficiario indicado:
    //  - Si existe, la asignacion queda vinculada de una a esa cuenta
    //    (UsuarioId completo) y Estado arranca en Pendiente.
    //  - Si NO existe todavia ninguna cuenta con ese email, la asignacion
    //    queda con UsuarioId en null (invitacion sin reclamar) hasta que esa
    //    persona se registre con ese mismo email (ver
    //    UsuarioService.CrearUsuarioAsync).
    //
    // Puede lanzar:
    //  - RecursoNoEncontradoException: si el activoDigitalId no existe.
    //  - ReglaNegocioException: si algun porcentaje es invalido, si la suma
    //    de porcentajes (existentes + nuevos) supera el 100%, si el otorgante
    //    intenta asignarse el activo a si mismo, o si ocurre un error
    //    tecnico al persistir.
    Task<IEnumerable<AsignacionHerenciaDTO>> CrearAsignacionesAsync(
        int activoDigitalId,
        IEnumerable<AsignacionHerenciaCreacionDTO> asignacionesCreacionDTO);

    // Actualiza el Porcentaje/Condicion de una unica asignacion existente,
    // validando que el nuevo porcentaje (reemplazando el actual de esa misma
    // asignacion) no haga superar el 100% acumulado del ActivoDigital.
    Task<AsignacionHerenciaDTO> ActualizarAsignacionAsync(int id, AsignacionHerenciaActualizacionDTO asignacionActualizacionDTO);

    // Elimina una asignacion existente.
    Task EliminarAsignacionAsync(int id);

    // CambiarEstadoAsync: implementa el flujo de ACEPTACION/RECHAZO de la
    // herencia digital, pasando la designacion de "Pendiente" a un estado
    // FINAL: "Aceptado" o "Rechazado". Es un metodo COMPARTIDO por dos
    // puntos de entrada con modelos de confianza distintos (ver el
    // comentario detallado en la implementacion): el PATCH autenticado de
    // AsignacionesController (que exige que quien llama sea el Usuario
    // beneficiario, verificado por JWT) y el flujo publico de
    // InvitacionesController (que confia en quien conoce el link recibido
    // por Email, funcione o no esa persona todavia con una cuenta creada).
    // Por eso este metodo NO exige que "asignacionId" ya este vinculado a un
    // Usuario: esa exigencia, cuando corresponde, la aplica el CONTROLLER
    // que la invoca.
    //
    // Puede lanzar:
    //  - RecursoNoEncontradoException: si el asignacionId no existe.
    //  - ReglaNegocioException: si el estado actual de la asignacion YA es
    //    "Aceptado" o "Rechazado" (una decision final no puede revisarse ni
    //    revertirse), o si "nuevoEstado" es "Pendiente" (no tiene sentido
    //    "volver atras" a un estado que solo el sistema asigna
    //    automaticamente al crear la asignacion).
    Task<AsignacionHerenciaDTO> CambiarEstadoAsync(int asignacionId, EstadoBeneficiario nuevoEstado);

    // CambiarEstadoPorTokenAsync: variante PUBLICA de CambiarEstadoAsync,
    // que identifica la asignacion por su TokenInvitacion en vez de por su
    // Id entero interno. La usa InvitacionesController.ProcesarInvitacion
    // (sin login). Mismas reglas de negocio y mismas excepciones que
    // CambiarEstadoAsync.
    Task<AsignacionHerenciaDTO> CambiarEstadoPorTokenAsync(string token, EstadoBeneficiario nuevoEstado);
}
