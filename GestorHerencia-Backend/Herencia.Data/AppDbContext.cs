using Herencia.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Herencia.Data;

// AppDbContext es el "puente" entre nuestras clases C# (Models) y la base de datos
// SQLite. EF Core usa esta clase para saber que tablas crear (a traves de las
// migraciones Code-First) y como traducir LINQ a SQL.
public class AppDbContext : DbContext
{
    // El constructor recibe las DbContextOptions (cadena de conexion, proveedor, etc.)
    // desde el contenedor de Inyeccion de Dependencias configurado en la capa Api.
    // La capa Data NO decide la cadena de conexion: solo la recibe.
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Cada DbSet<T> representa una tabla en la base de datos.
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Beneficiario> Beneficiarios => Set<Beneficiario>();
    public DbSet<ActivoDigital> ActivosDigitales => Set<ActivoDigital>();
    public DbSet<AsignacionHerencia> AsignacionesHerencia => Set<AsignacionHerencia>();

    // OnModelCreating es el lugar donde, con Fluent API, le decimos EXPLICITAMENTE
    // a EF Core como mapear cada entidad: claves primarias, foraneas, longitudes
    // maximas, indices, comportamiento ante borrados y datos semilla (seeders).
    // Usamos Fluent API (en vez de solo Data Annotations) porque nos da control
    // total y mantiene las clases de dominio (Models) limpias de detalles de
    // infraestructura/persistencia.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ==========================================================
        // 1) CONFIGURACION DE Usuario
        // ==========================================================
        modelBuilder.Entity<Usuario>(entity =>
        {
            // Clave primaria explicita (aunque EF ya la detectaria por convencion,
            // dejarla explicita documenta la intencion para quien lea el codigo).
            entity.HasKey(u => u.Id);

            // Nombre obligatorio con longitud maxima razonable para evitar
            // columnas de texto ilimitado en la base de datos.
            entity.Property(u => u.Nombre)
                  .IsRequired()
                  .HasMaxLength(150);

            // Email obligatorio. Ademas creamos un indice UNICO: dos usuarios
            // no pueden registrarse con el mismo email (regla de negocio basica
            // de cualquier sistema de autenticacion).
            entity.Property(u => u.Email)
                  .IsRequired()
                  .HasMaxLength(150);

            entity.HasIndex(u => u.Email)
                  .IsUnique();

            // PasswordHash y PasswordSalt son obligatorios: un Usuario no puede
            // existir sin sus credenciales de seguridad calculadas.
            entity.Property(u => u.PasswordHash)
                  .IsRequired();

            entity.Property(u => u.PasswordSalt)
                  .IsRequired();

            // Rol es obligatorio (todo Usuario tiene SIEMPRE un nivel de
            // permisos definido, nunca "sin rol"). EF Core persiste el enum
            // como INTEGER por defecto (0 = Usuario, 1 = Administrador).
            entity.Property(u => u.Rol)
                  .IsRequired();

            // --- Relacion 1-N: Usuario (1) -> Beneficiario (N) ---
            // Un usuario tiene muchos beneficiarios; cada beneficiario pertenece
            // a un unico usuario. Si se borra el Usuario, se borran en cascada
            // sus Beneficiarios (no tiene sentido un beneficiario huerfano).
            entity.HasMany(u => u.Beneficiarios)
                  .WithOne(b => b.Usuario)
                  .HasForeignKey(b => b.UsuarioId)
                  .OnDelete(DeleteBehavior.Cascade);

            // --- Relacion 1-N: Usuario (1) -> ActivoDigital (N) ---
            // Misma logica: un usuario es dueno de muchos activos digitales.
            entity.HasMany(u => u.ActivosDigitales)
                  .WithOne(a => a.Usuario)
                  .HasForeignKey(a => a.UsuarioId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ==========================================================
        // 2) CONFIGURACION DE Beneficiario
        // ==========================================================
        modelBuilder.Entity<Beneficiario>(entity =>
        {
            entity.HasKey(b => b.Id);

            entity.Property(b => b.Nombre)
                  .IsRequired()
                  .HasMaxLength(150);

            entity.Property(b => b.Email)
                  .IsRequired()
                  .HasMaxLength(150);

            entity.Property(b => b.Parentesco)
                  .HasMaxLength(100);
        });

        // ==========================================================
        // 3) CONFIGURACION DE ActivoDigital
        // ==========================================================
        modelBuilder.Entity<ActivoDigital>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.Nombre)
                  .IsRequired()
                  .HasMaxLength(150);

            // El enum TipoActivoDigital se guarda por defecto como INTEGER en SQLite;
            // no hace falta una conversion explicita, pero lo dejamos como Required
            // para que la columna no admita NULL.
            entity.Property(a => a.Tipo)
                  .IsRequired();

            entity.Property(a => a.Descripcion)
                  .HasMaxLength(500);
        });

        // ==========================================================
        // 4) CONFIGURACION DE AsignacionHerencia (tabla intermedia N-N)
        // ==========================================================
        modelBuilder.Entity<AsignacionHerencia>(entity =>
        {
            entity.HasKey(x => x.Id);

            // decimal(5,2): permite valores como 100.00 (3 enteros + 2 decimales).
            // Usamos HasPrecision en vez de dejar el default porque SQLite/EF
            // necesita saber la precision para no truncar el porcentaje.
            entity.Property(x => x.PorcentajeAsignado)
                  .HasPrecision(5, 2)
                  .IsRequired();

            entity.Property(x => x.CondicionLiberacion)
                  .HasMaxLength(300);

            // --- Lado N-N, parte 1: AsignacionHerencia -> ActivoDigital ---
            // Si se borra un ActivoDigital, se borran en cascada las asignaciones
            // que lo referencian (ya no tiene sentido conservarlas).
            entity.HasOne(x => x.ActivoDigital)
                  .WithMany(a => a.AsignacionesHerencia)
                  .HasForeignKey(x => x.ActivoDigitalId)
                  .OnDelete(DeleteBehavior.Cascade);

            // --- Lado N-N, parte 2: AsignacionHerencia -> Beneficiario ---
            // Aqui usamos Restrict (en vez de Cascade) a proposito: si intentamos
            // borrar un Beneficiario que todavia tiene asignaciones activas, la
            // base de datos rechaza el borrado. Esto evita perder informacion de
            // "a quien y en que porcentaje se le asigno un activo" por accidente,
            // y de paso evita un escenario de MULTIPLES CAMINOS DE CASCADA hacia
            // AsignacionHerencia (uno via ActivoDigital y otro via Beneficiario),
            // que EF Core podria rechazar al generar la migracion.
            entity.HasOne(x => x.Beneficiario)
                  .WithMany(b => b.AsignacionesHerencia)
                  .HasForeignKey(x => x.BeneficiarioId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ==========================================================
        // 5) SEEDERS (HasData) - datos de prueba
        // ==========================================================
        // IMPORTANTE: HasData exige valores ESTATICOS y solo propiedades escalares
        // (no se pueden asignar objetos de navegacion). Por eso cada semilla fija
        // manualmente sus FKs (UsuarioId, ActivoDigitalId, BeneficiarioId) y usa
        // una fecha fija (no DateTime.Now/UtcNow) para que la migracion generada
        // sea siempre identica y reproducible entre entornos.
        var fechaSeed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Hash/salt de ejemplo: en un escenario real estos bytes los calcula la
        // capa Business (ej: HMACSHA512) antes de guardar el Usuario. Aqui usamos
        // valores fijos solo para poder sembrar usuarios de prueba sin depender
        // de logica de negocio dentro de la capa Data.
        var passwordHashSeed = Convert.FromBase64String("aGFzaERlUHJ1ZWJhU2VtaWxsYTEyMzQ1Ng==");
        var passwordSaltSeed = Convert.FromBase64String("c2FsdERlUHJ1ZWJhU2VtaWxsYTEyMzQ1Ng==");

        // --- 2 Usuarios de prueba ---
        // El usuario 1 se siembra como Administrador (para poder probar de
        // entrada el endpoint "GET /api/usuarios", restringido por rol) y el
        // usuario 2 como Usuario comun.
        modelBuilder.Entity<Usuario>().HasData(
            new Usuario
            {
                Id = 1,
                Nombre = "Maximiliano Miceli",
                Email = "maximiceli@hotmail.com.ar",
                PasswordHash = passwordHashSeed,
                PasswordSalt = passwordSaltSeed,
                Rol = RolUsuario.Administrador,
                FechaCreacion = fechaSeed,
                UsuarioCreacion = "seed"
            },
            new Usuario
            {
                Id = 2,
                Nombre = "Ana Torres",
                Email = "ana.torres@example.com",
                PasswordHash = passwordHashSeed,
                PasswordSalt = passwordSaltSeed,
                Rol = RolUsuario.Usuario,
                FechaCreacion = fechaSeed,
                UsuarioCreacion = "seed"
            }
        );

        // --- 4 Beneficiarios de prueba (2 por usuario) ---
        modelBuilder.Entity<Beneficiario>().HasData(
            new Beneficiario { Id = 1, Nombre = "Lucas Miceli", Email = "lucas.miceli@example.com", Parentesco = "Hijo", UsuarioId = 1, FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new Beneficiario { Id = 2, Nombre = "Sofia Miceli", Email = "sofia.miceli@example.com", Parentesco = "Hija", UsuarioId = 1, FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new Beneficiario { Id = 3, Nombre = "Carlos Torres", Email = "carlos.torres@example.com", Parentesco = "Conyuge", UsuarioId = 2, FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new Beneficiario { Id = 4, Nombre = "Julia Torres", Email = "julia.torres@example.com", Parentesco = "Hermana", UsuarioId = 2, FechaCreacion = fechaSeed, UsuarioCreacion = "seed" }
        );

        // --- 15 Activos Digitales de prueba ---
        // Cantidad pedida explicitamente por la rubrica para poder probar
        // paginado en los endpoints de la capa Api mas adelante.
        modelBuilder.Entity<ActivoDigital>().HasData(
            new ActivoDigital { Id = 1, Nombre = "Cuenta Banco Santander", Tipo = TipoActivoDigital.CuentaBancaria, Descripcion = "Caja de ahorro en pesos", UsuarioId = 1, FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new ActivoDigital { Id = 2, Nombre = "Cuenta Banco Galicia", Tipo = TipoActivoDigital.CuentaBancaria, Descripcion = "Cuenta corriente en dolares", UsuarioId = 1, FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new ActivoDigital { Id = 3, Nombre = "Instagram personal", Tipo = TipoActivoDigital.RedSocial, Descripcion = "Perfil publico con 5000 seguidores", UsuarioId = 1, FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new ActivoDigital { Id = 4, Nombre = "Facebook personal", Tipo = TipoActivoDigital.RedSocial, Descripcion = "Perfil familiar", UsuarioId = 1, FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new ActivoDigital { Id = 5, Nombre = "Billetera MetaMask", Tipo = TipoActivoDigital.BilleteraCripto, Descripcion = "Wallet Ethereum", UsuarioId = 1, FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new ActivoDigital { Id = 6, Nombre = "Billetera Binance", Tipo = TipoActivoDigital.BilleteraCripto, Descripcion = "Exchange con balance en USDT", UsuarioId = 1, FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new ActivoDigital { Id = 7, Nombre = "Correo Gmail principal", Tipo = TipoActivoDigital.CorreoElectronico, Descripcion = "Cuenta de correo con backups de fotos", UsuarioId = 1, FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new ActivoDigital { Id = 8, Nombre = "Correo Outlook laboral", Tipo = TipoActivoDigital.CorreoElectronico, Descripcion = "Cuenta de correo del trabajo anterior", UsuarioId = 1, FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new ActivoDigital { Id = 9, Nombre = "Dominio web personal", Tipo = TipoActivoDigital.Otro, Descripcion = "Dominio registrado en un proveedor DNS", UsuarioId = 1, FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new ActivoDigital { Id = 10, Nombre = "Cuenta Banco Nacion", Tipo = TipoActivoDigital.CuentaBancaria, Descripcion = "Caja de ahorro en pesos", UsuarioId = 2, FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new ActivoDigital { Id = 11, Nombre = "Cuenta Banco BBVA", Tipo = TipoActivoDigital.CuentaBancaria, Descripcion = "Plazo fijo renovable", UsuarioId = 2, FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new ActivoDigital { Id = 12, Nombre = "LinkedIn profesional", Tipo = TipoActivoDigital.RedSocial, Descripcion = "Perfil profesional con recomendaciones", UsuarioId = 2, FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new ActivoDigital { Id = 13, Nombre = "Billetera Ledger", Tipo = TipoActivoDigital.BilleteraCripto, Descripcion = "Hardware wallet con Bitcoin", UsuarioId = 2, FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new ActivoDigital { Id = 14, Nombre = "Correo Gmail secundario", Tipo = TipoActivoDigital.CorreoElectronico, Descripcion = "Cuenta de respaldo", UsuarioId = 2, FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new ActivoDigital { Id = 15, Nombre = "Cuenta Netflix", Tipo = TipoActivoDigital.Otro, Descripcion = "Suscripcion de streaming compartida", UsuarioId = 2, FechaCreacion = fechaSeed, UsuarioCreacion = "seed" }
        );

        // --- 5 Asignaciones de herencia de prueba (demuestran la relacion N-N) ---
        modelBuilder.Entity<AsignacionHerencia>().HasData(
            new AsignacionHerencia { Id = 1, ActivoDigitalId = 1, BeneficiarioId = 1, PorcentajeAsignado = 50.00m, CondicionLiberacion = "Certificado de defuncion + 30 dias", FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new AsignacionHerencia { Id = 2, ActivoDigitalId = 1, BeneficiarioId = 2, PorcentajeAsignado = 50.00m, CondicionLiberacion = "Certificado de defuncion + 30 dias", FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new AsignacionHerencia { Id = 3, ActivoDigitalId = 5, BeneficiarioId = 1, PorcentajeAsignado = 100.00m, CondicionLiberacion = "Certificado de defuncion", FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new AsignacionHerencia { Id = 4, ActivoDigitalId = 10, BeneficiarioId = 3, PorcentajeAsignado = 100.00m, CondicionLiberacion = "Certificado de defuncion", FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new AsignacionHerencia { Id = 5, ActivoDigitalId = 13, BeneficiarioId = 4, PorcentajeAsignado = 100.00m, CondicionLiberacion = "Mayoria de edad del beneficiario", FechaCreacion = fechaSeed, UsuarioCreacion = "seed" }
        );
    }
}
