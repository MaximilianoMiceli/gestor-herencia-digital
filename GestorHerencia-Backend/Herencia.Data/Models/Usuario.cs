namespace Herencia.Data.Models;

/// <summary>
/// Cualquier persona registrada en el sistema, con una unica cuenta (Email, PasswordHash/Salt
/// e Id unicos). La misma entidad sirve para OTORGANTE y BENEFICIARIO: un mismo Usuario puede
/// ser dueño de sus activos y, a la vez, beneficiario de otra herencia.
/// </summary>
public class Usuario : EntidadBaseAuditable
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>DNI del titular. String (no int) porque puede tener ceros a la izquierda.</summary>
    public string Dni { get; set; } = string.Empty;

    public DateTime FechaNacimiento { get; set; }

    public byte[] PasswordHash { get; set; } = [];

    public byte[] PasswordSalt { get; set; } = [];

    public RolUsuario Rol { get; set; } = RolUsuario.Usuario;

    /// <summary>Token de un solo uso para "olvide mi contraseña". Se invalida (null) al usarse o al pedir uno nuevo.</summary>
    public string? PasswordResetToken { get; set; }

    public DateTime? PasswordResetExpiracion { get; set; }

    public bool DobleFactorHabilitado { get; set; } = false;

    /// <summary>Codigo de 6 digitos de un solo uso para el login con 2FA. Vida corta, en texto plano.</summary>
    public string? CodigoDobleFactor { get; set; }

    public DateTime? CodigoDobleFactorExpiracion { get; set; }

    public ICollection<ActivoDigital> ActivosOtorgados { get; set; } = new List<ActivoDigital>();

    public ICollection<AsignacionHerencia> HerenciasRecibidas { get; set; } = new List<AsignacionHerencia>();
}
