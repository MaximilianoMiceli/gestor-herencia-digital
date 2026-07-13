using System.Text;
using Herencia.Api.Jobs;
using Herencia.Business.Interfaces;
using Herencia.Business.Services;
using Herencia.Data;
using Herencia.Data.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// CORS: se usa AllowAnyOrigin() porque el frontend (Expo/React Native) corre desde origenes
// cambiantes (emulador, dispositivo fisico via LAN, Expo Web con puerto variable). Esto es
// seguro SOLO porque esta Api no usa cookies/sesiones de navegador para autenticar (usa JWT
// Bearer adjuntado manualmente en el header Authorization): AllowAnyOrigin() + credenciales
// de cookies si seria peligroso, pero esa combinacion ni siquiera esta permitida por el
// framework. En un despliegue de produccion real corresponderia reemplazarlo por una lista
// explicita de dominios confiables.
const string politicaCors = "PermitirFrontend";

builder.Services.AddCors(options =>
{
    options.AddPolicy(politicaCors, policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Cadena de conexion de la capa Data (SQLite), leida desde appsettings.json.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Falta la cadena de conexion 'DefaultConnection' en la configuracion.");

// Si el "Data Source" configurado es una ruta relativa, se resuelve contra
// AppContext.BaseDirectory (la carpeta de los binarios de esta Api) en vez del working
// directory del proceso, que varia segun como se ejecute (dotnet run, publicado, servicio de
// Windows, acceso directo). Asi el archivo SQLite siempre queda en un lugar estable y
// predecible junto a la aplicacion.
var sqliteConnectionStringBuilder = new SqliteConnectionStringBuilder(connectionString);
if (!Path.IsPathRooted(sqliteConnectionStringBuilder.DataSource))
{
    sqliteConnectionStringBuilder.DataSource =
        Path.Combine(AppContext.BaseDirectory, sqliteConnectionStringBuilder.DataSource);
}

// Para un despliegue futuro (Docker, servidor con volumen persistente), alcanza con pisar
// "ConnectionStrings:DefaultConnection" con una ruta ya absoluta: solo se completa cuando es relativa.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(sqliteConnectionStringBuilder.ConnectionString));

// Repositorios (capa Data) registrados como Scoped, coherente con el ciclo de vida de AppDbContext.
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IActivoDigitalRepository, ActivoDigitalRepository>();
builder.Services.AddScoped<IAsignacionHerenciaRepository, AsignacionHerenciaRepository>();
builder.Services.AddScoped<IConfiguracionVerificacionVidaRepository, ConfiguracionVerificacionVidaRepository>();
builder.Services.AddScoped<ICertificadoDefuncionRepository, CertificadoDefuncionRepository>();

// Servicios de la capa Business.
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IActivoDigitalService, ActivoDigitalService>();
builder.Services.AddScoped<IAsignacionHerenciaService, AsignacionHerenciaService>();
builder.Services.AddScoped<IVerificacionVidaService, VerificacionVidaService>();
builder.Services.AddScoped<ICertificadoDefuncionService, CertificadoDefuncionService>();

// Servicios de infraestructura: notificaciones simuladas por consola (sin proveedor de email
// real integrado) y almacenamiento de archivos en disco local.
builder.Services.AddScoped<INotificationService, NotificacionSimuladaService>();
builder.Services.AddScoped<IAlmacenamientoArchivosService, AlmacenamientoLocalService>();

// Servicios de seguridad: hash/salt de contraseñas y emision de JWT.
builder.Services.AddScoped<ISeguridadService, SeguridadService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// Autenticacion JWT Bearer. Los parametros de validacion deben ser coherentes con como
// TokenService.CrearToken firma el token (misma clave, mismo algoritmo HMAC-SHA512).
var claveJwt = builder.Configuration["AppSettings:Token"]
    ?? throw new InvalidOperationException("Falta 'AppSettings:Token' en la configuracion.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveJwt)),
            ValidateLifetime = true,

            // Esta Api no emite Claims "iss"/"aud" en TokenService.CrearToken (solo serian
            // relevantes si varios servicios/clientes compartieran este esquema de
            // autenticacion), por lo que estas validaciones se desactivan para ser
            // coherentes con lo que el token realmente contiene.
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// Registra VerificacionVidaBackgroundService como hosted service: el propio host lo arranca y
// detiene junto con la Api, sin necesitar un disparador HTTP externo.
builder.Services.AddHostedService<VerificacionVidaBackgroundService>();

builder.Services.AddOpenApi();

// Swagger UI: capa de documentacion navegable sobre el documento OpenAPI generado por AddOpenApi().
builder.Services.AddSwaggerGen(options =>
{
    // Define el esquema "Bearer" que Swagger UI usa para adjuntar el JWT (obtenido de
    // POST /api/auth/login) en los requests de prueba.
    //
    // NOTA de version: Swashbuckle 10.x (compatible con .NET 10) usa Microsoft.OpenApi 2.x,
    // que reestructuro su API (elimino el sub-namespace ".Models" y reemplazo "objeto +
    // propiedad Reference" por clases "...Reference" dedicadas, ej: OpenApiSecuritySchemeReference).
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pegar UNICAMENTE el token JWT (sin el prefijo 'Bearer '), obtenido de POST /api/auth/login."
    });

    options.AddSecurityRequirement(document =>
    {
        var requerimiento = new OpenApiSecurityRequirement();
        var referenciaEsquemaBearer = new OpenApiSecuritySchemeReference("Bearer", document, null);
        requerimiento.Add(referenciaEsquemaBearer, []);
        return requerimiento;
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// UseCors() debe ir antes de UseAuthentication()/UseAuthorization(): el navegador necesita
// saber que el origen esta permitido independientemente de si el request resulta autenticado.
app.UseCors(politicaCors);

// Orden critico: Authentication (¿quien sos?, valida el JWT y arma el ClaimsPrincipal) debe
// ejecutarse siempre antes que Authorization (¿tenes permiso para esto?, evalua [Authorize]
// contra ese usuario ya identificado). Invertido, Authorization se ejecutaria sin saber quien
// es el usuario y ningun request pasaria, ni con un token valido.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Las migraciones se aplican automaticamente al iniciar para que el esquema de la base SQLite
// siempre este al dia sin un paso manual adicional (aceptable para el alcance de este proyecto).
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();
}

app.Run();
