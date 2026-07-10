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

    // Cada DbSet<T> representa una tabla en la base de datos. Notar que ya NO
    // existe un DbSet<Beneficiario>: ese rol lo cumple ahora el propio Usuario
    // (ver el comentario de "doble rol" en Usuario.cs).
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<ActivoDigital> ActivosDigitales => Set<ActivoDigital>();
    public DbSet<AsignacionHerencia> AsignacionesHerencia => Set<AsignacionHerencia>();
    public DbSet<ConfiguracionVerificacionVida> ConfiguracionesVerificacionVida => Set<ConfiguracionVerificacionVida>();
    public DbSet<CertificadoDefuncion> CertificadosDefuncion => Set<CertificadoDefuncion>();

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
            // de cualquier sistema de autenticacion, y ademas la LLAVE con la
            // que se "reclaman" automaticamente las invitaciones pendientes de
            // AsignacionHerencia.EmailInvitado al registrarse, ver
            // UsuarioService.CrearUsuarioAsync).
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

            // PasswordResetToken/PasswordResetExpiracion son OPCIONALES
            // (nullable en la entidad): la mayoria de los Usuarios no tiene
            // ningun reseteo de contraseña en curso. HasMaxLength acota el
            // tamaño de un valor que, al ser generado por el propio servidor
            // (ver UsuarioService.SolicitarResetPasswordAsync), tiene un
            // largo fijo y conocido de antemano.
            entity.Property(u => u.PasswordResetToken)
                  .HasMaxLength(100);

            // --- Relacion 1-N: Usuario (1, rol OTORGANTE) -> ActivoDigital (N) ---
            // Un usuario es dueño de muchos activos digitales; cada activo
            // pertenece a un unico otorgante. Si se borra el Usuario, se
            // borran en cascada sus ActivoDigital: no tiene sentido conservar
            // un activo "huerfano" sin ningun titular. Esta es la UNICA ruta
            // de cascada que nace directamente en Usuario hacia ActivoDigital,
            // por lo que Cascade aca no genera ninguna ambiguedad.
            entity.HasMany(u => u.ActivosOtorgados)
                  .WithOne(a => a.Usuario)
                  .HasForeignKey(a => a.UsuarioId)
                  .OnDelete(DeleteBehavior.Cascade);

            // La relacion 1-N: Usuario (1, rol BENEFICIARIO) -> AsignacionHerencia (N)
            // ("HerenciasRecibidas") se configura mas abajo, DESDE el lado de
            // AsignacionHerencia (seccion 3), junto con el resto de sus FKs:
            // asi quedan juntas, en un solo lugar, las DOS rutas de cascada
            // que llegan a esa misma tabla, que es justamente lo que hay que
            // analizar en conjunto para decidir cual debe ser Restrict (ver
            // el comentario detallado ahi).
        });

        // ==========================================================
        // 2) CONFIGURACION DE ActivoDigital
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
        // 3) CONFIGURACION DE AsignacionHerencia (tabla intermedia N-N)
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

            // EmailInvitado es obligatorio (SIEMPRE se completa, tenga o no
            // cuenta esa persona todavia: ver el comentario de la propiedad
            // en AsignacionHerencia.cs), con la misma longitud maxima que
            // Usuario.Email por tratarse del mismo tipo de dato.
            entity.Property(x => x.EmailInvitado)
                  .IsRequired()
                  .HasMaxLength(150);

            // Estado (enum EstadoBeneficiario) es OBLIGATORIO: toda asignacion
            // esta SIEMPRE en alguno de los 3 estados definidos, nunca "sin
            // estado". Se persiste como INTEGER (comportamiento por defecto
            // de EF Core para enums), por CONSISTENCIA con el resto de los
            // enums de este proyecto (RolUsuario, TipoActivoDigital) — la
            // alternativa seria ".HasConversion<string>()" (mas legible
            // mirando la tabla "a mano" con un cliente SQLite), pero se
            // descarto por ocupar mas espacio y ser mas lento de indexar.
            // HasDefaultValue es una segunda capa de defensa (ademas del
            // valor por defecto ya fijado en el propio modelo,
            // AsignacionHerencia.Estado = EstadoBeneficiario.Pendiente): si
            // una fila se inserta por fuera de la logica de negocio de
            // Business, la base de datos la completa con "Pendiente" en vez
            // de dejarla en un 0 que no corresponde a ningun miembro del enum.
            entity.Property(x => x.Estado)
                  .IsRequired()
                  .HasDefaultValue(EstadoBeneficiario.Pendiente);

            // TokenInvitacion es el identificador PUBLICO no adivinable
            // (ver el comentario detallado en AsignacionHerencia.cs). El
            // indice UNICO evita, a nivel de base de datos, que dos filas
            // terminen compartiendo el mismo token por un bug futuro en la
            // generacion aleatoria (colision practicamente imposible con
            // RandomNumberGenerator de 256 bits, pero la constraint es una
            // red de seguridad casi gratis).
            entity.Property(x => x.TokenInvitacion)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.HasIndex(x => x.TokenInvitacion)
                  .IsUnique();

            // --- Lado N-N, parte 1: AsignacionHerencia -> ActivoDigital ---
            // Si se borra un ActivoDigital, se borran en cascada las
            // asignaciones que lo referencian (ya no tiene sentido
            // conservarlas: el activo que reparten ya no existe).
            entity.HasOne(x => x.ActivoDigital)
                  .WithMany(a => a.AsignacionesHerencia)
                  .HasForeignKey(x => x.ActivoDigitalId)
                  .OnDelete(DeleteBehavior.Cascade);

            // --- Lado N-N, parte 2: AsignacionHerencia -> Usuario (rol BENEFICIARIO) ---
            //
            // IsRequired(false): a diferencia de ActivoDigitalId, esta FK es
            // OPCIONAL, porque UsuarioId es nullable (int?) en la entidad: el
            // otorgante puede invitar a alguien por Email que todavia no
            // tiene cuenta (ver AsignacionHerencia.EmailInvitado). Mientras
            // esa persona no se registre, esta columna vive en NULL.
            //
            // --- ¿Por que DeleteBehavior.Restrict (y no Cascade) aca? ---
            // Este mismo Usuario (tabla "Usuarios") ya tiene, desde arriba,
            // OTRA ruta de cascada hacia esta MISMA tabla AsignacionHerencia:
            // Usuario -[Cascade]-> ActivoDigital -[Cascade]-> AsignacionHerencia
            // (seccion 1 + la regla de arriba, en el rol OTORGANTE). Si
            // ADEMAS configuraramos esta segunda ruta,
            // Usuario -[Cascade]-> AsignacionHerencia (en el rol BENEFICIARIO),
            // quedarian DOS caminos de cascada distintos conviniendo en la
            // MISMA tabla, ambos originados en "Usuarios". Motores como SQL
            // Server rechazan directamente ese tipo de esquema al crear las
            // FKs ("may cause cycles or multiple cascade paths"); con SQLite,
            // ademas, el propio EF Core puede comportarse de forma ambigua o
            // fallar al generar la migracion ante este patron, especialmente
            // en el caso limite (aunque en teoria posible con este esquema)
            // de que un mismo Usuario sea, para una misma fila, TANTO el
            // otorgante (via su ActivoDigital) COMO el beneficiario (via esta
            // FK directa): borrar ese Usuario dispararia dos ordenes de
            // cascada distintas apuntando a la MISMA fila, algo que un motor
            // relacional no puede resolver de forma deterministica.
            //
            // Ademas de esa razon tecnica, hay una razon de NEGOCIO igual de
            // valida: si un Beneficiario decidiera eliminar su cuenta, no
            // queremos perder en silencio el registro de "que le fue
            // asignado y por quien" (el otorgante original podria seguir
            // necesitando esa informacion, por ejemplo para reasignarlo a
            // otra persona). Restrict fuerza a resolver esa situacion
            // explicitamente (reasignar o eliminar la AsignacionHerencia a
            // mano) antes de poder borrar la cuenta del beneficiario.
            entity.HasOne(x => x.Usuario)
                  .WithMany(u => u.HerenciasRecibidas)
                  .HasForeignKey(x => x.UsuarioId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ==========================================================
        // 4) CONFIGURACION DE ConfiguracionVerificacionVida (1-1 con Usuario)
        // ==========================================================
        modelBuilder.Entity<ConfiguracionVerificacionVida>(entity =>
        {
            // --- Clave primaria COMPARTIDA con Usuario ---
            // UsuarioId es, a la vez, la PK de esta tabla y la FK hacia
            // Usuario: es la forma estandar en EF Core de modelar una
            // relacion 1-1 OBLIGATORIA (un Usuario tiene, como maximo, una
            // sola fila de configuracion, nunca varias), sin necesitar un
            // Id autoincremental propio que nunca tendria sentido de negocio.
            entity.HasKey(c => c.UsuarioId);

            entity.Property(c => c.FrecuenciaMeses)
                  .IsRequired();

            entity.Property(c => c.Metodo)
                  .IsRequired();

            entity.Property(c => c.UltimoCheckIn)
                  .IsRequired();

            // Mismo criterio que AsignacionHerencia.Estado: default a nivel
            // de base de datos como segunda capa de defensa ademas del
            // default ya fijado en el modelo (Estado = Activo).
            entity.Property(c => c.Estado)
                  .IsRequired()
                  .HasDefaultValue(EstadoVerificacionVida.Activo);

            // --- FK 1: UsuarioId (PK compartida) -> Usuario (rol TITULAR) ---
            // Cascade: si se borra el Usuario, no tiene sentido conservar
            // "huerfana" su propia configuracion de monitoreo. A diferencia
            // del caso de AsignacionHerencia (ver seccion 3), esta es la
            // UNICA ruta de cascada que llega a esta tabla desde Usuario
            // (la otra FK de abajo, ContactoConfianzaId, va con Restrict),
            // asi que Cascade aca no genera ninguna ambiguedad.
            entity.HasOne(c => c.Usuario)
                  .WithOne()
                  .HasForeignKey<ConfiguracionVerificacionVida>(c => c.UsuarioId)
                  .OnDelete(DeleteBehavior.Cascade);

            // --- FK 2: ContactoConfianzaId -> Usuario (rol CONTACTO) ---
            // Segunda FK hacia la MISMA tabla Usuarios: igual que
            // AsignacionHerencia.UsuarioId (ver el comentario detallado en
            // la seccion 3), debe ir con Restrict para no generar una
            // segunda ruta de cascada convergente sobre esta tabla. Ademas,
            // de negocio: si el contacto de confianza elimina su cuenta, no
            // queremos que la configuracion del titular desaparezca en
            // silencio junto con el (obligaria a elegir uno nuevo, no a
            // perder toda la configuracion de frecuencia/metodo).
            entity.HasOne(c => c.ContactoConfianza)
                  .WithMany()
                  .HasForeignKey(c => c.ContactoConfianzaId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ==========================================================
        // 5) CONFIGURACION DE CertificadoDefuncion
        // ==========================================================
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

            // --- TRES FKs distintas hacia la MISMA tabla Usuarios ---
            // (titular fallecido, heredero que subio el documento, admin
            // que lo reviso). Las TRES van con Restrict, por el mismo
            // motivo ya justificado en AsignacionHerencia.UsuarioId (seccion
            // 3) y en ConfiguracionVerificacionVida.ContactoConfianzaId
            // (seccion 4): con multiples FKs hacia una misma tabla, a lo
            // sumo UNA puede ir en Cascade sin generar rutas convergentes
            // ambiguas, y esta tabla en particular no tiene ninguna razon
            // de negocio para borrar certificados en cascada al eliminar
            // CUALQUIERA de los tres usuarios involucrados: son, ante todo,
            // un registro de auditoria que debe sobrevivir a la baja de
            // cualquiera de las cuentas que participaron.
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

        // ==========================================================
        // 6) SEEDERS (HasData) - datos de prueba
        // ==========================================================
        // IMPORTANTE: HasData exige valores ESTATICOS y solo propiedades escalares
        // (no se pueden asignar objetos de navegacion). Por eso cada semilla fija
        // manualmente sus FKs (UsuarioId, ActivoDigitalId) y usa una fecha fija
        // (no DateTime.Now/UtcNow) para que la migracion generada sea siempre
        // identica y reproducible entre entornos.
        var fechaSeed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Hash/salt de ejemplo: en un escenario real estos bytes los calcula la
        // capa Business (ej: HMACSHA512) antes de guardar el Usuario. Aqui usamos
        // valores fijos solo para poder sembrar usuarios de prueba sin depender
        // de logica de negocio dentro de la capa Data.
        var passwordHashSeed = Convert.FromBase64String("aGFzaERlUHJ1ZWJhU2VtaWxsYTEyMzQ1Ng==");
        var passwordSaltSeed = Convert.FromBase64String("c2FsdERlUHJ1ZWJhU2VtaWxsYTEyMzQ1Ng==");

        // --- 3 Usuarios de prueba ---
        // Los 3 son necesarios para demostrar el modelo de DOBLE ROL con una
        // cadena real: Usuario 1 (Otorgante) le deja un activo a Usuario 2
        // (que ahi actua de Beneficiario), y ESE MISMO Usuario 2 (ahora en su
        // rol de Otorgante) le deja, a su vez, un activo a Usuario 3 (ver la
        // seccion de AsignacionHerencia mas abajo). El Usuario 1 se siembra
        // como Administrador (para poder probar de entrada el endpoint "GET
        // /api/usuarios", restringido por rol); los otros dos, como Usuario comun.
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
            },
            new Usuario
            {
                Id = 3,
                Nombre = "Carlos Sosa",
                Email = "carlos.sosa@example.com",
                PasswordHash = passwordHashSeed,
                PasswordSalt = passwordSaltSeed,
                Rol = RolUsuario.Usuario,
                FechaCreacion = fechaSeed,
                UsuarioCreacion = "seed"
            }
        );

        // --- 17 Activos Digitales de prueba (>= 15 pedidos por la rubrica) ---
        // Repartidos entre los 3 usuarios (9 + 6 + 2) para que los tres
        // puedan actuar como otorgantes en algun momento del escenario de
        // prueba, no solo como beneficiarios.
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

        // --- 5 Asignaciones de herencia de prueba ---
        // Demuestran, en conjunto:
        //  - El reparto N-N de un mismo activo entre 2 beneficiarios (Id 1 y 2).
        //  - La cadena pedida por la rubrica "Usuario A le deja un activo a
        //    Usuario B, y Usuario B le deja uno a Usuario C": Id 1 es
        //    Maximiliano(1, Otorgante) -> Ana(2, Beneficiario), e Id 3 es
        //    Ana(2, Otorgante) -> Carlos(3, Beneficiario). Ana aparece asi
        //    simultaneamente en AMBOS roles, la esencia misma del modelo de
        //    doble rol.
        //  - Una invitacion a alguien SIN CUENTA todavia (Id 4: UsuarioId
        //    null + EmailInvitado), que el dia que esa persona se registre
        //    con ese mismo email quedara reclamada automaticamente (ver
        //    UsuarioService.CrearUsuarioAsync).
        //  - Una asignacion YA RESUELTA de antemano (Id 5, Estado = Aceptado),
        //    util para probar de entrada la regla critica de
        //    AsignacionHerenciaService.CambiarEstadoAsync ("el estado ya fue
        //    procesado y no puede modificarse") sin depender de haber llamado
        //    antes al endpoint PATCH.
        // NOTA sobre TokenInvitacion en los seeds: HasData exige valores
        // ESTATICOS conocidos en tiempo de compilacion (no se puede llamar a
        // RandomNumberGenerator aca), asi que se fijan strings arbitrarios
        // pero unicos "a mano" para las 5 filas semilla. En la operacion
        // normal de la Api (AsignacionHerenciaService.CrearAsignacionesAsync)
        // este valor SI se genera aleatoriamente en cada alta.
        modelBuilder.Entity<AsignacionHerencia>().HasData(
            new AsignacionHerencia { Id = 1, ActivoDigitalId = 1, UsuarioId = 2, EmailInvitado = "ana.torres@example.com", PorcentajeAsignado = 50.00m, CondicionLiberacion = "Certificado de defuncion + 30 dias", Estado = EstadoBeneficiario.Pendiente, TokenInvitacion = "seed-token-0000000000000000000000000000001", FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new AsignacionHerencia { Id = 2, ActivoDigitalId = 1, UsuarioId = 3, EmailInvitado = "carlos.sosa@example.com", PorcentajeAsignado = 50.00m, CondicionLiberacion = "Certificado de defuncion + 30 dias", Estado = EstadoBeneficiario.Pendiente, TokenInvitacion = "seed-token-0000000000000000000000000000002", FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new AsignacionHerencia { Id = 3, ActivoDigitalId = 10, UsuarioId = 3, EmailInvitado = "carlos.sosa@example.com", PorcentajeAsignado = 100.00m, CondicionLiberacion = "Certificado de defuncion", Estado = EstadoBeneficiario.Pendiente, TokenInvitacion = "seed-token-0000000000000000000000000000003", FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new AsignacionHerencia { Id = 4, ActivoDigitalId = 5, UsuarioId = null, EmailInvitado = "invitado.sinregistro@example.com", PorcentajeAsignado = 100.00m, CondicionLiberacion = "Certificado de defuncion", Estado = EstadoBeneficiario.Pendiente, TokenInvitacion = "seed-token-0000000000000000000000000000004", FechaCreacion = fechaSeed, UsuarioCreacion = "seed" },
            new AsignacionHerencia { Id = 5, ActivoDigitalId = 13, UsuarioId = 1, EmailInvitado = "maximiceli@hotmail.com.ar", PorcentajeAsignado = 100.00m, CondicionLiberacion = "Mayoria de edad del beneficiario", Estado = EstadoBeneficiario.Aceptado, TokenInvitacion = "seed-token-0000000000000000000000000000005", FechaCreacion = fechaSeed, UsuarioCreacion = "seed" }
        );
    }
}
