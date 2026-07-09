namespace Herencia.Data.Models;

// Representa a la persona que recibira uno o mas activos digitales
// cuando se cumpla la condicion de liberacion de la herencia.
public class Beneficiario : EntidadBaseAuditable
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    // Relacion del beneficiario con el titular (ej: "Hijo/a", "Conyuge", "Amigo/a").
    // Es informativa, no forma parte de ninguna relacion de EF.
    public string Parentesco { get; set; } = string.Empty;

    // Estado del flujo de aceptacion de la herencia digital (ver enum
    // EstadoBeneficiario para el detalle de por que se modela asi).
    //
    // Se elige modelar este campo ACA, en Beneficiario, y no en la tabla
    // intermedia AsignacionHerencia, porque "asignar un beneficiario" (el
    // evento que dispara el estado "Pendiente", segun la regla de negocio)
    // ocurre en POST /api/beneficiarios: el momento en que el usuario titular
    // DESIGNA a una persona como beneficiario suyo, relacion 1-N Usuario ->
    // Beneficiario. AsignacionHerencia, en cambio, modela algo distinto: COMO
    // SE REPARTE un ActivoDigital puntual (porcentaje, condicion de
    // liberacion) entre beneficiarios YA designados. La aceptacion o rechazo
    // de la designacion como beneficiario es logicamente ANTERIOR y
    // INDEPENDIENTE de cuantos o cuales activos se le terminen asignando
    // despues, por lo que corresponde al Beneficiario en si, no a cada fila
    // de reparto.
    //
    // El valor por defecto "= EstadoBeneficiario.Pendiente" se aplica
    // automaticamente apenas se instancia un Beneficiario en memoria (ej. en
    // BeneficiarioService.CrearBeneficiarioAsync, sin necesidad de que ese
    // metodo lo asigne explicitamente), cumpliendo la regla de negocio:
    // "cuando un usuario titular asigna a un beneficiario, este debe quedar
    // en estado Pendiente".
    public EstadoBeneficiario Estado { get; set; } = EstadoBeneficiario.Pendiente;

    // --- Lado "N" de la relacion 1-N Usuario -> Beneficiario ---
    // FK explicita hacia el Usuario que registro a este beneficiario.
    // La modelamos explicitamente (en vez de dejar que EF la infiera) para poder
    // configurarla con claridad en el Fluent API (OnDelete, requerido, etc).
    public int UsuarioId { get; set; }

    // Propiedad de navegacion hacia el Usuario dueno de este beneficiario.
    public Usuario Usuario { get; set; } = null!;

    // --- Lado "N-N" de la relacion ActivoDigital <-> Beneficiario ---
    // Un mismo Beneficiario puede estar en muchas asignaciones (recibir varios activos),
    // y esta coleccion es el punto de entrada para navegar esa relacion muchos-a-muchos
    // a traves de la tabla intermedia AsignacionHerencia.
    public ICollection<AsignacionHerencia> AsignacionesHerencia { get; set; } = new List<AsignacionHerencia>();
}
