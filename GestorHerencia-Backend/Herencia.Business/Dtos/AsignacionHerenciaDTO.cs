namespace Herencia.Business.Dtos;

// AsignacionHerenciaDTO es el "contrato" de salida de una AsignacionHerencia:
// la fila de la tabla intermedia N-N entre ActivoDigital y Beneficiario, con
// los datos propios de la relacion (PorcentajeAsignado, CondicionLiberacion).
public class AsignacionHerenciaDTO
{
    public int Id { get; set; }

    public int ActivoDigitalId { get; set; }

    public int BeneficiarioId { get; set; }

    public decimal PorcentajeAsignado { get; set; }

    public string CondicionLiberacion { get; set; } = string.Empty;

    // UsuarioId del TITULAR del ActivoDigital (no un campo propio de la
    // entidad AsignacionHerencia: se completa a partir de
    // asignacion.ActivoDigital.UsuarioId). Se expone aca para que la capa Api
    // pueda resolver la verificacion de OWNERSHIP ("¿el usuario autenticado
    // es dueño de este ActivoDigital?") sin tener que hacer una consulta
    // adicional aparte contra IActivoDigitalService.
    public int UsuarioId { get; set; }
}
