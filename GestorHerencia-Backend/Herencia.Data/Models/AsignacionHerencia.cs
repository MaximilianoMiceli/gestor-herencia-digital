namespace Herencia.Data.Models;

/// <summary>
/// Tabla intermedia que resuelve la relacion N-N entre ActivoDigital y Usuario: cada
/// fila es un reparto puntual ("este ActivoDigital le corresponde, en tal porcentaje
/// y bajo tal condicion, a este Usuario actuando como Beneficiario").
/// </summary>
// Los dos lados de la relacion apuntan a la misma tabla Usuarios pero con roles
// distintos: ActivoDigital.UsuarioId (indirecto) identifica al OTORGANTE, y
// AsignacionHerencia.UsuarioId (directo) identifica al BENEFICIARIO. Un mismo
// Usuario puede aparecer en ambos roles a la vez (ver el seed en AppDbContext).
// Se modela como clase explicita, no como tabla intermedia implicita de EF, porque
// necesita datos propios (porcentaje, condicion, estado de aceptacion).
public class AsignacionHerencia : EntidadBaseAuditable
{
    public int Id { get; set; }

    public int ActivoDigitalId { get; set; }

    public ActivoDigital ActivoDigital { get; set; } = null!;

    // Nullable: el otorgante puede invitar como beneficiario a alguien por Email sin
    // que esa persona tenga cuenta todavia. Cuando se registra con ese mismo email,
    // UsuarioService.CrearUsuarioAsync reclama esta fila y completa el campo.

    /// <summary>FK hacia el Usuario beneficiario. Null mientras la persona invitada no tenga cuenta.</summary>
    public int? UsuarioId { get; set; }

    public Usuario? Usuario { get; set; }

    /// <summary>
    /// Email con el que se invito a esta persona. Se completa siempre (tenga o no
    /// cuenta) porque sirve de dato de contacto y de llave de reclamo automatico.
    /// </summary>
    public string EmailInvitado { get; set; } = string.Empty;

    // decimal (no float/double): es un valor proporcional donde importa la
    // precision exacta, sin errores de redondeo binario.

    /// <summary>Porcentaje del activo asignado a este beneficiario (0 a 100).</summary>
    public decimal PorcentajeAsignado { get; set; }

    /// <summary>Condicion para liberar el activo (ej: "Certificado de defuncion + 30 dias sin objeciones").</summary>
    public string CondicionLiberacion { get; set; } = string.Empty;

    // Vive en AsignacionHerencia (no en Usuario) porque un mismo beneficiario puede
    // recibir varias herencias de distintos otorgantes y aceptar unas y rechazar
    // otras de forma independiente: es un atributo de la relacion, no de la persona.

    /// <summary>Estado del flujo de aceptacion de esta asignacion puntual (ver EstadoBeneficiario).</summary>
    public EstadoBeneficiario Estado { get; set; } = EstadoBeneficiario.Pendiente;

    // Nullable y deliberadamente independiente de Estado: una fila puede quedar
    // meses en Aceptado sin que el otorgante haya fallecido, y solo se completa
    // tras la aprobacion del certificado de defuncion (ver CertificadoDefuncionService.AprobarAsync).

    /// <summary>Momento en que se liberaron los bienes hacia el beneficiario, o null si aun no ocurrio.</summary>
    public DateTime? FechaLiberacion { get; set; }

    // Identificador publico no adivinable, generado en
    // AsignacionHerenciaService.CrearAsignacionesAsync, usado en los endpoints
    // publicos de InvitacionesController (que no pueden verificar ownership por JWT
    // porque el invitado puede no tener cuenta todavia). El Id interno se sigue
    // usando en el resto de la Api, protegido por [Authorize].

    /// <summary>Token de alta entropia usado en los endpoints publicos de invitacion (en vez del Id interno).</summary>
    public string TokenInvitacion { get; set; } = string.Empty;
}
