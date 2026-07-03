namespace Herencia.Data.Models;

// Representa el activo digital a heredar: puede ser una cuenta bancaria,
// una red social, una billetera de criptomonedas, un correo electronico, etc.
public class ActivoDigital : EntidadBaseAuditable
{
    public int Id { get; set; }

    // Nombre descriptivo del activo (ej: "Cuenta Banco Santander", "Instagram personal").
    public string Nombre { get; set; } = string.Empty;

    // Categoria del activo, usando el enum TipoActivoDigital para evitar valores libres invalidos.
    public TipoActivoDigital Tipo { get; set; }

    // Descripcion/notas adicionales sobre el activo (ej: instrucciones de acceso,
    // plataforma, numero de cuenta parcial, etc).
    public string Descripcion { get; set; } = string.Empty;

    // --- Lado "N" de la relacion 1-N Usuario -> ActivoDigital ---
    // FK hacia el Usuario propietario del activo.
    public int UsuarioId { get; set; }

    public Usuario Usuario { get; set; } = null!;

    // --- Lado "N-N" de la relacion ActivoDigital <-> Beneficiario ---
    // Un mismo ActivoDigital puede repartirse entre varios Beneficiarios
    // (ej: 50% para un hijo, 50% para otro), de ahi que necesitemos la tabla
    // intermedia AsignacionHerencia en vez de una FK simple.
    public ICollection<AsignacionHerencia> AsignacionesHerencia { get; set; } = new List<AsignacionHerencia>();
}
