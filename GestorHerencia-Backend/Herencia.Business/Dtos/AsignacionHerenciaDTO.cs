using Herencia.Data.Models;

namespace Herencia.Business.Dtos;

// AsignacionHerenciaDTO es el "contrato" de salida de una AsignacionHerencia:
// la fila de la tabla intermedia N-N entre ActivoDigital y Usuario, con los
// datos propios de la relacion (PorcentajeAsignado, CondicionLiberacion,
// Estado).
public class AsignacionHerenciaDTO
{
    public int Id { get; set; }

    public int ActivoDigitalId { get; set; }

    // Id del Usuario BENEFICIARIO, o null si la persona invitada todavia no
    // se registro en el sistema (ver AsignacionHerencia.UsuarioId y
    // EmailInvitado). Se llama "UsuarioBeneficiarioId" (y no simplemente
    // "UsuarioId", como en la entidad) para no confundirlo con
    // "UsuarioOtorganteId" mas abajo: dentro de este UNICO DTO conviven los
    // Id de AMBOS roles (otorgante y beneficiario), y ambos son, en la base,
    // Ids de la misma tabla Usuarios.
    public int? UsuarioBeneficiarioId { get; set; }

    // Email con el que se invito a esta persona como beneficiaria (tenga o
    // no cuenta todavia). Se expone siempre, incluso cuando
    // UsuarioBeneficiarioId ya esta completo, para que el cliente pueda
    // mostrarlo sin una consulta aparte.
    public string EmailInvitado { get; set; } = string.Empty;

    public decimal PorcentajeAsignado { get; set; }

    public string CondicionLiberacion { get; set; } = string.Empty;

    // Estado del flujo de aceptacion de ESTA asignacion puntual (ver
    // EstadoBeneficiario).
    public EstadoBeneficiario Estado { get; set; }

    // Id del Usuario OTORGANTE (el titular del ActivoDigital al que
    // pertenece esta asignacion). No es una propiedad propia de la entidad
    // AsignacionHerencia: se completa a partir de
    // asignacion.ActivoDigital.UsuarioId. Se expone aca para que la capa Api
    // pueda resolver la verificacion de OWNERSHIP del lado del otorgante
    // ("¿el usuario autenticado es el dueño del ActivoDigital?") sin una
    // consulta adicional aparte contra IActivoDigitalService.
    public int UsuarioOtorganteId { get; set; }

    // Identificador PUBLICO no adivinable de esta invitacion (ver el
    // comentario detallado en AsignacionHerencia.TokenInvitacion). Se expone
    // aca para que el controller que crea la asignacion
    // (ActivosDigitalesController.CrearAsignaciones) pueda armar el link de
    // invitacion con ESTE valor, nunca con "Id".
    public string TokenInvitacion { get; set; } = string.Empty;
}
