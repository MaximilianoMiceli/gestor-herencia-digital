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
