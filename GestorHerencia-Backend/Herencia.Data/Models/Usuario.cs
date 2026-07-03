namespace Herencia.Data.Models;

// Representa al titular de la cuenta: la persona duena de los activos digitales
// y que define quienes son sus beneficiarios. Hereda de EntidadBaseAuditable
// para tener trazabilidad de creacion/modificacion.
public class Usuario : EntidadBaseAuditable
{
    // Clave primaria. La convencion "Id" es reconocida automaticamente por EF Core
    // (Conventions) como PK, pero igual la reforzamos explicitamente en el Fluent API.
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    // --- Seguridad de contrasenas: NUNCA se guarda la contrasena en texto plano. ---
    // PasswordHash: resultado de aplicar un algoritmo de hashing (ej: HMACSHA512 o PBKDF2)
    // sobre la contrasena original concatenada/combinada con el "salt".
    // Se guarda como arreglo de bytes porque el hash es informacion binaria, no texto.
    public byte[] PasswordHash { get; set; } = [];

    // PasswordSalt: valor aleatorio unico generado por usuario, usado como entrada extra
    // del algoritmo de hashing. Su proposito es evitar que dos usuarios con la misma
    // contrasena tengan el mismo hash, y frustrar ataques de "rainbow tables".
    // El calculo real (generar salt + hashear) se hace en la capa Business, no aqui:
    // la capa Data solo se encarga de PERSISTIR estos valores ya calculados.
    public byte[] PasswordSalt { get; set; } = [];

    // --- Relaciones 1-N ---
    // Un Usuario puede tener muchos Beneficiarios registrados (relacion 1-N obligatoria
    // pedida por la rubrica). La coleccion se inicializa vacia para evitar NullReferenceException
    // al navegar la relacion antes de que EF la cargue (lazy/eager loading).
    public ICollection<Beneficiario> Beneficiarios { get; set; } = new List<Beneficiario>();

    // Un Usuario tambien puede ser dueno de muchos ActivoDigital (otra relacion 1-N,
    // coherente con el dominio: "un usuario tiene muchas cuentas/billeteras/redes sociales").
    public ICollection<ActivoDigital> ActivosDigitales { get; set; } = new List<ActivoDigital>();
}
