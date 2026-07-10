namespace Herencia.Data.Models;

// Representa a CUALQUIER persona registrada en el sistema, con UNA UNICA
// cuenta (un unico Email, un unico PasswordHash/PasswordSalt, un unico Id).
//
// --- ¿Por que la MISMA entidad Usuario sirve para "Otorgante" Y "Beneficiario"? ---
// En versiones anteriores de este modelo existia una entidad separada
// "Beneficiario" (sin credenciales propias): un simple registro de
// Nombre/Email/Parentesco creado por el titular. Ese diseño generaba una
// friccion real: el mismo ser humano podia terminar representado por DOS
// filas distintas en la base de datos (una fila "Usuario" si el mismo se
// registraba con cuenta propia, y otra fila "Beneficiario" separada si
// alguien mas lo designaba como heredero), sin ninguna relacion formal entre
// ambas. Eso obligaba a "linkear" ambas identidades mas adelante (por
// email) con logica ad-hoc, y no dejaba modelar con naturalidad el caso mas
// comun del dominio real: una persona que es DUEÑA de sus propios activos
// digitales (rol de OTORGANTE) Y AL MISMO TIEMPO fue designada por otra
// persona para heredar los suyos (rol de BENEFICIARIO). Ambos roles no son
// mutuamente excluyentes ni pertenecen a "tipos" de persona distintos: son
// simplemente los DOS EXTREMOS posibles de una misma relacion
// (AsignacionHerencia), y cualquier Usuario puede aparecer en cualquiera de
// los dos extremos, incluso en varias filas distintas al mismo tiempo (ej:
// Juan es Otorgante de un activo para Maria, y a la vez Beneficiario de un
// activo que le dejo Pedro).
//
// Modelar un UNICO "Usuario" con dos colecciones de navegacion (una por cada
// rol) refleja esa realidad con fidelidad, evita la duplicacion de identidad
// digital (una sola cuenta, una sola contraseña, un solo Email por persona) y
// elimina de raiz la necesidad de "fusionar" registros el dia que un
// Beneficiario finalmente decide crearse una cuenta propia.
//
// Hereda de EntidadBaseAuditable para tener trazabilidad de creacion/modificacion.
public class Usuario : EntidadBaseAuditable
{
    // Clave primaria. La convencion "Id" es reconocida automaticamente por EF Core
    // (Conventions) como PK, pero igual la reforzamos explicitamente en el Fluent API.
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    // --- Seguridad de contrasenas: NUNCA se guarda la contrasena en texto plano. ---
    // PasswordHash: resultado de aplicar un algoritmo de hashing (ej: HMACSHA512 o PBKDF2)
    // sobre la contrasena original combinada con el "salt".
    // Se guarda como arreglo de bytes porque el hash es informacion binaria, no texto.
    public byte[] PasswordHash { get; set; } = [];

    // PasswordSalt: valor aleatorio unico generado por usuario, usado como entrada extra
    // del algoritmo de hashing. Su proposito es evitar que dos usuarios con la misma
    // contrasena tengan el mismo hash, y frustrar ataques de "rainbow tables".
    public byte[] PasswordSalt { get; set; } = [];

    // Nivel de permisos del usuario dentro del sistema. Por defecto,
    // RolUsuario.Usuario: solo el auto-registro publico crea usuarios, y
    // siempre con este rol basico, nunca con privilegios elevados.
    public RolUsuario Rol { get; set; } = RolUsuario.Usuario;

    // --- Flujo de "olvide mi contraseña" ---
    // PasswordResetToken: un valor aleatorio de UN SOLO USO que se genera en
    // UsuarioService.SolicitarResetPasswordAsync y se envia (simulado, por
    // consola) al Email del usuario. Es nullable porque la INMENSA mayoria
    // del tiempo un Usuario NO tiene ningun reseteo en curso: solo se
    // completa temporalmente mientras dura la ventana de reseteo, y se
    // vuelve a limpiar (null) apenas se usa (o se pide uno nuevo, que
    // invalida al anterior).
    //
    // ¿Por que no reutilizar directamente un JWT para esto? Un JWT firmado
    // por TokenService no se puede "revocar" individualmente (ver el
    // comentario de Expires en TokenService.CrearToken): si se filtrara el
    // link de reseteo, seguiria siendo valido hasta expirar. Guardando el
    // token de reseteo en la base de datos, en cambio, se puede invalidar en
    // cualquier momento (poniendolo en null) apenas se usa una vez, o si el
    // usuario pide otro nuevo antes de usar el primero.
    public string? PasswordResetToken { get; set; }

    // Fecha de expiracion del PasswordResetToken de arriba. Un link de
    // reseteo de contraseña debe ser de vida CORTA (a diferencia del propio
    // Usuario): si alguien accede a una bandeja de entrada vieja, no
    // deberia poder resetear la contraseña con un link de hace meses.
    public DateTime? PasswordResetExpiracion { get; set; }

    // --- ROL 1: OTORGANTE ---
    // Los ActivoDigital que ESTE usuario registro como propios y que,
    // eventualmente, va a repartir entre sus beneficiarios. Es el lado "1" de
    // la relacion 1-N Usuario(Otorgante) -> ActivoDigital: un otorgante puede
    // tener muchos activos, pero cada activo pertenece a un unico otorgante.
    public ICollection<ActivoDigital> ActivosOtorgados { get; set; } = new List<ActivoDigital>();

    // --- ROL 2: BENEFICIARIO ---
    // Las AsignacionHerencia en las que ESTE usuario fue designado como
    // destinatario de un activo ajeno (de OTRO usuario, el otorgante de esa
    // fila puntual). Notar que esta coleccion NO tiene relacion directa con
    // "ActivosOtorgados": son dos colecciones independientes que conviven en
    // el mismo Usuario, una por cada rol posible. Un mismo Usuario puede
    // tener CERO, una o muchas herencias recibidas, cada una de un otorgante
    // distinto (o incluso del mismo), y puede aceptar unas y rechazar otras
    // de forma completamente independiente (ver EstadoBeneficiario en
    // AsignacionHerencia.Estado).
    public ICollection<AsignacionHerencia> HerenciasRecibidas { get; set; } = new List<AsignacionHerencia>();
}
