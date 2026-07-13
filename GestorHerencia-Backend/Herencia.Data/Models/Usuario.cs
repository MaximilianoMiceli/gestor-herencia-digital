namespace Herencia.Data.Models;

/// <summary>
/// Cualquier persona registrada en el sistema, con una unica cuenta (Email,
/// PasswordHash/Salt e Id unicos).
/// </summary>
// La misma entidad Usuario sirve para OTORGANTE y BENEFICIARIO. En una version
// anterior existia una entidad "Beneficiario" separada, sin credenciales propias,
// lo que obligaba a "linkear" por email dos filas de la misma persona cuando esta
// terminaba creandose una cuenta propia. Un unico Usuario con dos colecciones de
// navegacion (una por rol) refleja que ambos roles no son excluyentes -una misma
// persona puede ser dueña de sus activos y, a la vez, beneficiaria de otra- y
// elimina la necesidad de fusionar identidades.
public class Usuario : EntidadBaseAuditable
{
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    // Se exige en el alta (UsuarioService.CrearUsuarioAsync) porque el sistema
    // custodia informacion critica de terceros y decide a quien libera esos
    // bienes: saber con certeza quien es cada cuenta no es opcional. String (no
    // int) porque puede tener ceros a la izquierda y nunca se opera aritmeticamente.

    /// <summary>Documento Nacional de Identidad del titular.</summary>
    public string Dni { get; set; } = string.Empty;

    /// <summary>Necesaria para validar mayoria de edad al registrarse (ver UsuarioService).</summary>
    public DateTime FechaNacimiento { get; set; }

    /// <summary>Hash de la contraseña (nunca se guarda en texto plano). Binario: arreglo de bytes.</summary>
    public byte[] PasswordHash { get; set; } = [];

    /// <summary>Valor aleatorio unico por usuario usado en el hashing, para evitar hashes iguales entre contraseñas iguales.</summary>
    public byte[] PasswordSalt { get; set; } = [];

    public RolUsuario Rol { get; set; } = RolUsuario.Usuario;

    // Nullable: la mayoria del tiempo un Usuario no tiene ningun reseteo en curso.
    // No se reutiliza un JWT para esto porque un JWT firmado no se puede revocar
    // individualmente (ver Expires en TokenService.CrearToken); este token, en
    // cambio, se invalida (null) apenas se usa o se pide uno nuevo.

    /// <summary>Token de un solo uso para el flujo de "olvide mi contraseña" (ver UsuarioService.SolicitarResetPasswordAsync).</summary>
    public string? PasswordResetToken { get; set; }

    /// <summary>Expiracion de PasswordResetToken: un link de reseteo debe ser de vida corta.</summary>
    public DateTime? PasswordResetExpiracion { get; set; }

    /// <summary>Si el usuario activo el segundo factor por email desde su perfil.</summary>
    public bool DobleFactorHabilitado { get; set; } = false;

    // Se guarda en texto plano (no hasheado), igual que PasswordResetToken: su
    // seguridad depende de ser de vida corta y de un solo uso, no de resistir un
    // volcado de la base de datos.

    /// <summary>Codigo de 6 digitos de un solo uso para el login con 2FA (ver UsuarioService.GenerarYEnviarCodigoDobleFactorAsync).</summary>
    public string? CodigoDobleFactor { get; set; }

    /// <summary>Expiracion del CodigoDobleFactor (ventana corta, 10 minutos).</summary>
    public DateTime? CodigoDobleFactorExpiracion { get; set; }

    /// <summary>Rol OTORGANTE: activos que este usuario registro como propios.</summary>
    public ICollection<ActivoDigital> ActivosOtorgados { get; set; } = new List<ActivoDigital>();

    /// <summary>Rol BENEFICIARIO: asignaciones en las que este usuario fue designado destinatario de un activo ajeno.</summary>
    public ICollection<AsignacionHerencia> HerenciasRecibidas { get; set; } = new List<AsignacionHerencia>();
}
