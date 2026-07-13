using Herencia.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Herencia.Data;

/// <summary>
/// Puente entre los Models de C# y la base de datos SQLite: EF Core la usa para
/// generar migraciones Code-First y traducir LINQ a SQL.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Ya no existe un DbSet<Beneficiario>: ese rol lo cumple el propio Usuario (ver Usuario.cs).
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<ActivoDigital> ActivosDigitales => Set<ActivoDigital>();
    public DbSet<AsignacionHerencia> AsignacionesHerencia => Set<AsignacionHerencia>();
    public DbSet<ConfiguracionVerificacionVida> ConfiguracionesVerificacionVida => Set<ConfiguracionVerificacionVida>();
    public DbSet<CertificadoDefuncion> CertificadosDefuncion => Set<CertificadoDefuncion>();

    // Fluent API (no solo Data Annotations) para mantener los Models libres de detalles de persistencia.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Usuario ---
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Nombre)
                  .IsRequired()
                  .HasMaxLength(150);

            entity.Property(u => u.Email)
                  .IsRequired()
                  .HasMaxLength(150);

            // Unico: regla basica de autenticacion, y ademas la llave con la que se reclaman
            // automaticamente las invitaciones pendientes de AsignacionHerencia.EmailInvitado.
            entity.HasIndex(u => u.Email)
                  .IsUnique();

            entity.Property(u => u.Dni)
                  .IsRequired()
                  .HasMaxLength(15);

            // Unico: nadie puede registrarse dos veces con el mismo documento.
            entity.HasIndex(u => u.Dni)
                  .IsUnique();

            entity.Property(u => u.FechaNacimiento)
                  .IsRequired();

            entity.Property(u => u.PasswordHash)
                  .IsRequired();

            entity.Property(u => u.PasswordSalt)
                  .IsRequired();

            entity.Property(u => u.Rol)
                  .IsRequired();

            entity.Property(u => u.PasswordResetToken)
                  .HasMaxLength(100);

            // Cascade: si se borra el Usuario, no tiene sentido conservar sus ActivoDigital huerfanos.
            entity.HasMany(u => u.ActivosOtorgados)
                  .WithOne(a => a.Usuario)
                  .HasForeignKey(a => a.UsuarioId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // --- ActivoDigital ---
        modelBuilder.Entity<ActivoDigital>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.Nombre)
                  .IsRequired()
                  .HasMaxLength(150);

            entity.Property(a => a.Tipo)
                  .IsRequired();

            entity.Property(a => a.Descripcion)
                  .HasMaxLength(500);
        });

        // --- AsignacionHerencia (tabla intermedia N-N) ---
        modelBuilder.Entity<AsignacionHerencia>(entity =>
        {
            entity.HasKey(x => x.Id);

            // HasPrecision explicito: 5,2 admite hasta 100.00 sin truncar el porcentaje.
            entity.Property(x => x.PorcentajeAsignado)
                  .HasPrecision(5, 2)
                  .IsRequired();

            entity.Property(x => x.CondicionLiberacion)
                  .HasMaxLength(300);

            entity.Property(x => x.EmailInvitado)
                  .IsRequired()
                  .HasMaxLength(150);

            // HasDefaultValue como segunda capa de defensa: si una fila se inserta por fuera
            // de la logica de negocio, la base la completa en "Pendiente" y no en un 0 invalido.
            entity.Property(x => x.Estado)
                  .IsRequired()
                  .HasDefaultValue(EstadoBeneficiario.Pendiente);

            entity.Property(x => x.TokenInvitacion)
                  .IsRequired()
                  .HasMaxLength(100);

            // Unico: evita que dos filas compartan token por un bug futuro en la generacion aleatoria.
            entity.HasIndex(x => x.TokenInvitacion)
                  .IsUnique();

            // Cascade: si se borra el ActivoDigital, sus asignaciones ya no tienen sentido.
            entity.HasOne(x => x.ActivoDigital)
                  .WithMany(a => a.AsignacionesHerencia)
                  .HasForeignKey(x => x.ActivoDigitalId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Restrict (no Cascade): ya existe otra ruta de cascada Usuario->ActivoDigital->AsignacionHerencia;
            // una segunda ruta directa generaria cascada ambigua. De negocio: si el beneficiario borra su
            // cuenta, no queremos perder en silencio el registro de que le fue asignado el activo.
            entity.HasOne(x => x.Usuario)
                  .WithMany(u => u.HerenciasRecibidas)
                  .HasForeignKey(x => x.UsuarioId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // --- ConfiguracionVerificacionVida (1-1 con Usuario) ---
        modelBuilder.Entity<ConfiguracionVerificacionVida>(entity =>
        {
            // PK compartida con Usuario: forma estandar de EF Core para una relacion 1-1
            // obligatoria sin un Id autoincremental sin sentido de negocio propio.
            entity.HasKey(c => c.UsuarioId);

            entity.Property(c => c.FrecuenciaMeses)
                  .IsRequired();

            entity.Property(c => c.Metodo)
                  .IsRequired();

            entity.Property(c => c.UltimoCheckIn)
                  .IsRequired();

            entity.Property(c => c.Estado)
                  .IsRequired()
                  .HasDefaultValue(EstadoVerificacionVida.Activo);

            // Cascade: si se borra el Usuario, no tiene sentido conservar
            // huerfana su propia configuracion. Es la unica ruta Cascade hacia esta tabla.
            entity.HasOne(c => c.Usuario)
                  .WithOne()
                  .HasForeignKey<ConfiguracionVerificacionVida>(c => c.UsuarioId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Restrict (segunda FK hacia Usuarios: solo una ruta puede ir en Cascade). De negocio:
            // si el contacto de confianza borra su cuenta, no queremos perder la configuracion del titular.
            entity.HasOne(c => c.ContactoConfianza)
                  .WithMany()
                  .HasForeignKey(c => c.ContactoConfianzaId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // --- CertificadoDefuncion ---
        modelBuilder.Entity<CertificadoDefuncion>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.RutaArchivo)
                  .IsRequired()
                  .HasMaxLength(300);

            entity.Property(c => c.NombreArchivoOriginal)
                  .IsRequired()
                  .HasMaxLength(260);

            entity.Property(c => c.Estado)
                  .IsRequired()
                  .HasDefaultValue(EstadoCertificadoDefuncion.Pendiente);

            entity.Property(c => c.MotivoRechazo)
                  .HasMaxLength(500);

            // Tres FKs hacia Usuarios (titular, heredero, admin), todas Restrict: es un registro
            // de auditoria que debe sobrevivir a la baja de cualquiera de los tres.
            entity.HasOne(c => c.UsuarioTitular)
                  .WithMany()
                  .HasForeignKey(c => c.UsuarioTitularId)
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.SubidoPor)
                  .WithMany()
                  .HasForeignKey(c => c.SubidoPorUsuarioId)
                  .IsRequired()
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.RevisadoPor)
                  .WithMany()
                  .HasForeignKey(c => c.RevisadoPorUsuarioId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Seeders (HasData) - datos de prueba ---
        // HasData exige valores estaticos y una fecha fija (no DateTime.Now/UtcNow) para
        // que la migracion generada sea siempre identica y reproducible.
        var fechaSeed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Hash/salt reales (HMACSHA512) para poder loguearse de verdad. Contraseña en texto
        // plano de los 3 usuarios de prueba: "Test123456!"
        var passwordHashSeed = Convert.FromBase64String("7lDUenhjxBwPX/DKjqDlkN4PIKL8ydsEt3aTmhITAoBXodJ0wdibMst4wfaWliKF6CeW51ys8aI3tBm4C4eRuw==");
        var passwordSaltSeed = Convert.FromBase64String("HHYRrwJEzysVkjRbfTwwuY628CYOQv9HgbWh5w3jOl9YRsO/9lz1t6a+dPFFe0oO479Mdtn4KPNYpZ/N4hajHdTehwBvW+6pWR4uVmsC4eJ7GB/zgWA4VngYKcunwnffJalF6V9pC2IzEhQeiUvWXybgDsz0IxSIAxM65VFKqTU=");

        // Los 3 usuarios demuestran el doble rol con una cadena real: Usuario 1 (Otorgante)
        // le deja un activo a Usuario 2, y este, ahora de Otorgante, le deja uno a Usuario 3.
        modelBuilder.Entity<Usuario>().HasData(
            new Usuario
            {
                Id = 1,
                Nombre = "Maximiliano Miceli",
                Email = "maximiceli@hotmail.com.ar",
                Dni = "30111222",
                FechaNacimiento = new DateTime(1988, 3, 14, 0, 0, 0, DateTimeKind.Utc),
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
                Dni = "28555666",
                FechaNacimiento = new DateTime(1982, 7, 22, 0, 0, 0, DateTimeKind.Utc),
                PasswordHash = passwordHashSeed,
                PasswordSalt = passwordSaltSeed,
                Rol = RolUsuario.Usuario,
                FechaCreacion = fechaSeed,
                UsuarioCreacion = "seed"
            },
            new Usuario
            {
                Id = 3,
                Nombre = "Carlos Sosa",
                Email = "carlos.sosa@example.com",
                Dni = "33999888",
                FechaNacimiento = new DateTime(1995, 11, 5, 0, 0, 0, DateTimeKind.Utc),
                PasswordHash = passwordHashSeed,
                PasswordSalt = passwordSaltSeed,
                Rol = RolUsuario.Usuario,
                FechaCreacion = fechaSeed,
                UsuarioCreacion = "seed"
            }
        );

        // 17 activos repartidos 9/6/2 para que los tres usuarios actuen como otorgantes.
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
            new ActivoDigital { Id = 15, Nombre = "Cuenta Netflix", Tipo = TipoActivoDigital.Otro, Descripcion = "Suscripcion de streaming compartida", UsuarioId = 2, FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new ActivoDigital { Id = 16, Nombre = "Cuenta Banco Macro", Tipo = TipoActivoDigital.CuentaBancaria, Descripcion = "Caja de ahorro en pesos", UsuarioId = 3, FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new ActivoDigital { Id = 17, Nombre = "Cuenta X (Twitter)", Tipo = TipoActivoDigital.RedSocial, Descripcion = "Perfil personal", UsuarioId = 3, FechaCreacion = fechaSeed, UsuarioCreacion = "seed" }
        );

        // 5 asignaciones: reparto N-N de un activo entre 2 beneficiarios (Id 1 y 2), la cadena
        // Otorgante->Beneficiario->Otorgante (Id 1 y 3), una invitacion sin cuenta (Id 4) y una
        // ya Aceptada de antemano (Id 5). TokenInvitacion se fija a mano por exigencia de HasData.
        modelBuilder.Entity<AsignacionHerencia>().HasData(
            new AsignacionHerencia { Id = 1, ActivoDigitalId = 1, UsuarioId = 2, EmailInvitado = "ana.torres@example.com", PorcentajeAsignado = 50.00m, CondicionLiberacion = "Certificado de defuncion + 30 dias", Estado = EstadoBeneficiario.Pendiente, TokenInvitacion = "seed-token-0000000000000000000000000000001", FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new AsignacionHerencia { Id = 2, ActivoDigitalId = 1, UsuarioId = 3, EmailInvitado = "carlos.sosa@example.com", PorcentajeAsignado = 50.00m, CondicionLiberacion = "Certificado de defuncion + 30 dias", Estado = EstadoBeneficiario.Pendiente, TokenInvitacion = "seed-token-0000000000000000000000000000002", FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new AsignacionHerencia { Id = 3, ActivoDigitalId = 10, UsuarioId = 3, EmailInvitado = "carlos.sosa@example.com", PorcentajeAsignado = 100.00m, CondicionLiberacion = "Certificado de defuncion", Estado = EstadoBeneficiario.Pendiente, TokenInvitacion = "seed-token-0000000000000000000000000000003", FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new AsignacionHerencia { Id = 4, ActivoDigitalId = 5, UsuarioId = null, EmailInvitado = "invitado.sinregistro@example.com", PorcentajeAsignado = 100.00m, CondicionLiberacion = "Certificado de defuncion", Estado = EstadoBeneficiario.Pendiente, TokenInvitacion = "seed-token-0000000000000000000000000000004", FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new AsignacionHerencia { Id = 5, ActivoDigitalId = 13, UsuarioId = 1, EmailInvitado = "maximiceli@hotmail.com.ar", PorcentajeAsignado = 100.00m, CondicionLiberacion = "Mayoria de edad del beneficiario", Estado = EstadoBeneficiario.Aceptado, TokenInvitacion = "seed-token-0000000000000000000000000000005", FechaCreacion = fechaSeed, UsuarioCreacion = "seed" }
        );
    }
}
