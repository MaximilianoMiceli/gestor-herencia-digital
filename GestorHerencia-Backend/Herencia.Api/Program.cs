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

// AllowAnyOrigin() porque el frontend (Expo/React Native) corre desde origenes cambiantes.
// Seguro solo porque esta Api autentica con JWT Bearer manual, no cookies de sesion.
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

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Falta la cadena de conexion 'DefaultConnection' en la configuracion.");

// Si "Data Source" es una ruta relativa, se resuelve contra AppContext.BaseDirectory (no el
// working directory del proceso, que varia segun como se ejecute la Api).
var sqliteConnectionStringBuilder = new SqliteConnectionStringBuilder(connectionString);
if (!Path.IsPathRooted(sqliteConnectionStringBuilder.DataSource))
{
    sqliteConnectionStringBuilder.DataSource =
        Path.Combine(AppContext.BaseDirectory, sqliteConnectionStringBuilder.DataSource);
}

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

            // TokenService.CrearToken no emite Claims "iss"/"aud", por eso se desactivan aca.
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
    // Esquema "Bearer" que Swagger UI usa para adjuntar el JWT en los requests de prueba.
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

// UseCors() debe ir antes de Authentication/Authorization.
app.UseCors(politicaCors);

// Orden critico: Authentication (valida el JWT) siempre antes que Authorization ([Authorize]).
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Las migraciones se aplican automaticamente al iniciar.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();
}

app.Run();
