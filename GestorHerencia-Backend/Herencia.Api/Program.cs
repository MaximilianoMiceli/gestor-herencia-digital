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

// --- CORS (Cross-Origin Resource Sharing) ---
// El navegador (o el motor web de Expo, cuando la app corre en modo "web")
// aplica, por defecto, la politica de "mismo origen" (Same-Origin Policy):
// un sitio servido desde un origen (protocolo+dominio+puerto, ej:
// "http://localhost:8081", donde corre Metro/Expo) tiene PROHIBIDO, por
// defecto, hacer requests HTTP hacia un origen DISTINTO (ej: esta Api en
// "https://localhost:5001" o "http://10.0.2.2:5000"), aunque el usuario este
// autenticado y el request sea legitimo. Esto es una proteccion de
// SEGURIDAD del navegador (evita que un sitio malicioso haga requests a
// escondidas hacia otro sitio usando las credenciales del usuario), pero
// tambien bloquea, por accidente, la comunicacion legitima entre el
// frontend y esta Api si viven en origenes distintos, que es exactamente
// el caso de un frontend Expo/React Native corriendo por separado.
//
// CORS es el mecanismo por el cual el SERVIDOR (esta Api) le informa
// explicitamente al navegador "esta bien, confio en estos origenes, dejalos
// pasar". Sin este bloque, cualquier prueba del frontend en modo web
// (Expo Web) fallaria con un error de CORS en la consola del navegador,
// incluso con el token correcto.
//
// Se usa AllowAnyOrigin() (en vez de listar dominios especificos) porque
// este es un proyecto academico en desarrollo activo, donde el frontend
// puede correr desde multiples origenes cambiantes (emulador Android,
// dispositivo fisico via Expo Go con IP de LAN variable, Expo Web en
// localhost con puerto variable, etc.). Esto es seguro de hacer SOLO porque
// esta Api NO usa cookies ni sesiones basadas en el navegador para
// autenticar (usa JWT Bearer, que el cliente adjunta manualmente en el
// header "Authorization"): CORS con AllowAnyOrigin() JUNTO CON
// AllowCredentials() (cookies) si seria peligroso, pero esa combinacion ni
// siquiera esta permitida por el propio framework. Para un despliegue de
// produccion real, lo correcto seria reemplazar AllowAnyOrigin() por una
// lista explicita de dominios confiables (ej: el dominio del frontend ya
// publicado).
const string politicaCors = "PermitirFrontend";

builder.Services.AddCors(options =>
{
    options.AddPolicy(politicaCors, policy =>
    {
        policy
            .AllowAnyOrigin()   // acepta requests desde cualquier origen (ver justificacion arriba)
            .AllowAnyMethod()   // permite GET/POST/PUT/DELETE/etc., no solo GET
            .AllowAnyHeader();  // permite headers custom, incluido "Authorization: Bearer <token>"
    });
});

// --- Registro de la capa Data: AppDbContext ---
// Se configura EF Core con SQLite, leyendo la cadena de conexion desde
// appsettings.json (seccion "ConnectionStrings:DefaultConnection"). Este es
// el UNICO lugar de toda la solucion donde se decide el motor de base de
// datos y la cadena de conexion real: ni Business ni Api conocen este detalle
// en ningun otro punto, lo reciben ya resuelto via Inyeccion de Dependencias.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Falta la cadena de conexion 'DefaultConnection' en la configuracion.");

// SQLite guarda la base de datos en un simple ARCHIVO (a diferencia de un motor
// cliente-servidor como SQL Server). Si el "Data Source" configurado es una
// ruta RELATIVA (ej: "herencia_digital.db", como esta en appsettings.json), el
// driver la resuelve contra el WORKING DIRECTORY del proceso en el momento de
// ejecutar. Ese working directory NO es estable: es distinto si se ejecuta con
// "dotnet run" (carpeta del proyecto), desde el ejecutable ya publicado (carpeta
// de publicacion), desde un servicio de Windows (podria ser System32) o desde un
// acceso directo. Esto puede hacer que la Api, sin querer, cree o busque el
// archivo .db en un lugar distinto cada vez.
//
// Para evitar esa ambiguedad, resolvemos el "Data Source" a una ruta ABSOLUTA
// anclada en "AppContext.BaseDirectory": la carpeta donde fisicamente estan los
// binarios (.dll/.exe) de ESTA Api en ejecucion. Esa carpeta si es estable para
// un mismo despliegue, sin importar desde donde se invoque el proceso. De esta
// forma, el archivo SQLite siempre queda junto a la aplicacion que lo usa.
//
// Se usa SqliteConnectionStringBuilder (en vez de manipular el string a mano)
// para parsear/reescribir el "Data Source" de forma segura, respetando el
// resto de posibles parametros de la cadena de conexion (ej: "Cache=Shared").
var sqliteConnectionStringBuilder = new SqliteConnectionStringBuilder(connectionString);
if (!Path.IsPathRooted(sqliteConnectionStringBuilder.DataSource))
{
    sqliteConnectionStringBuilder.DataSource =
        Path.Combine(AppContext.BaseDirectory, sqliteConnectionStringBuilder.DataSource);
}

// NOTA para el dia de despliegue: si mas adelante la Api corre en un contenedor
// Docker o en un servidor donde se quiera que el archivo viva en otra ubicacion
// (ej: un volumen persistente), alcanza con pisar "ConnectionStrings:DefaultConnection"
// con una ruta ABSOLUTA (por variable de entorno o appsettings.Production.json):
// como recien arriba solo completamos la ruta cuando es RELATIVA, una ruta ya
// absoluta se respeta tal cual, sin tocar una sola linea de este archivo.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(sqliteConnectionStringBuilder.ConnectionString));

// --- Registro de la capa Data: Repositorios ---
// Se registran las implementaciones CONCRETAS (UsuarioRepository,
// ActivoDigitalRepository) contra sus respectivas INTERFACES. AddScoped
// crea una instancia nueva por cada request HTTP entrante y la reutiliza
// durante toda esa request (coherente con el ciclo de vida de AppDbContext,
// que tambien es Scoped por defecto con AddDbContext).
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IActivoDigitalRepository, ActivoDigitalRepository>();
builder.Services.AddScoped<IAsignacionHerenciaRepository, AsignacionHerenciaRepository>();
builder.Services.AddScoped<IConfiguracionVerificacionVidaRepository, ConfiguracionVerificacionVidaRepository>();
builder.Services.AddScoped<ICertificadoDefuncionRepository, CertificadoDefuncionRepository>();

// --- Registro de la capa Business: Servicios ---
// Igual criterio: se registran las implementaciones concretas contra sus
// interfaces (IUsuarioService, IActivoDigitalService, IAsignacionHerenciaService).
// Los controllers de la capa Api SOLO van a pedir estas interfaces por
// constructor; el contenedor de DI es el unico responsable de saber que
// implementacion concreta les corresponde inyectar.
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IActivoDigitalService, ActivoDigitalService>();
builder.Services.AddScoped<IAsignacionHerenciaService, AsignacionHerenciaService>();
builder.Services.AddScoped<IVerificacionVidaService, VerificacionVidaService>();
builder.Services.AddScoped<ICertificadoDefuncionService, CertificadoDefuncionService>();

// --- Servicios de infraestructura del monitoreo de verificacion de vida ---
// INotificationService (notificaciones simuladas por consola, ver el
// comentario detallado en NotificacionSimuladaService) e
// IAlmacenamientoArchivosService (guardado de certificados en disco local,
// ver AlmacenamientoLocalService) son, igual que ISeguridadService/
// ITokenService, servicios utilitarios sin dependencia de AppDbContext:
// se registran igual como Scoped por consistencia con el resto de la
// solucion.
builder.Services.AddScoped<INotificationService, NotificacionSimuladaService>();
builder.Services.AddScoped<IAlmacenamientoArchivosService, AlmacenamientoLocalService>();

// --- Registro de la capa Business: Servicios de seguridad ---
// ISeguridadService (hash/salt de contrasenas) y ITokenService (emision de
// JWT) son servicios UTILITARIOS PUROS: no dependen de ningun repositorio ni
// de AppDbContext, por lo que tecnicamente podrian registrarse incluso como
// Singleton. Se dejan como Scoped por consistencia con el resto de los
// servicios de la solucion (mismo ciclo de vida por request).
builder.Services.AddScoped<ISeguridadService, SeguridadService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// --- Autenticacion: JWT Bearer ---
// Le decimos a ASP.NET Core que el esquema de autenticacion de esta Api es
// "JWT Bearer": cuando un endpoint tenga el atributo [Authorize] (ej: todo
// ActivosDigitalesController), el MIDDLEWARE de Authentication (agregado mas
// abajo con app.UseAuthentication()) va a buscar, en cada request entrante,
// el header HTTP "Authorization: Bearer <token>", y va a validar ese token
// usando las reglas de TokenValidationParameters de abajo, ANTES de que el
// request llegue al codigo de ningun controller.
var claveJwt = builder.Configuration["AppSettings:Token"]
    ?? throw new InvalidOperationException("Falta 'AppSettings:Token' en la configuracion.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // TokenValidationParameters describe COMO se valida un token
        // entrante. Estos parametros deben ser coherentes con como
        // TokenService.CrearToken firmo el token originalmente (misma clave
        // secreta, mismo algoritmo simetrico HMAC-SHA512), o la validacion
        // fallaria siempre, incluso para tokens legitimos.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // ValidateIssuerSigningKey + IssuerSigningKey: la validacion mas
            // importante de todas. El middleware recalcula la firma del
            // token recibido usando ESTA MISMA clave secreta (la misma que
            // uso TokenService para firmar) y la compara contra la firma que
            // viaja en el token. Si no coinciden (token modificado a mano,
            // firmado con otra clave, o directamente inventado por un
            // atacante), el request se rechaza automaticamente con un 401,
            // sin que el codigo de ningun controller llegue a ejecutarse.
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveJwt)),

            // ValidateLifetime: rechaza automaticamente los tokens cuyo Claim
            // "exp" (fecha de expiracion, fijada en 2 horas por
            // TokenService.CrearToken) ya paso, sin que ningun controller
            // tenga que chequear manualmente si el token vencio.
            ValidateLifetime = true,

            // Esta Api todavia no emite Claims "iss" (issuer/emisor) ni "aud"
            // (audience/destinatario) en TokenService.CrearToken (solo serian
            // relevantes si, en el futuro, varios servicios o clientes
            // distintos compartieran este mismo esquema de autenticacion).
            // Se desactivan estas dos validaciones para ser coherentes con lo
            // que el token realmente contiene: activarlas sin emitir esos
            // Claims haria que TODO token, incluso uno recien emitido y
            // correctamente firmado, sea rechazado por "falta de issuer/audience".
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

// AddAuthorization registra los servicios internos que necesita el atributo
// [Authorize]: sin este llamado, [Authorize] no tendria una infraestructura
// de Autorizacion contra la cual evaluarse.
builder.Services.AddAuthorization();

// --- Registro de la capa Api: Controllers ---
// Habilita el uso de Controllers basados en atributos ([ApiController],
// [HttpGet], [Route], etc.), como UsuariosController y ActivosDigitalesController.
builder.Services.AddControllers();

// --- Background Service: escaneo periodico de verificacion de vida ---
// AddHostedService registra VerificacionVidaBackgroundService para que el
// propio host de ASP.NET Core lo arranque automaticamente al levantar la
// Api (y lo detenga prolijamente al apagarla), sin necesitar ningun
// disparador HTTP externo: es lo que permite detectar titulares vencidos
// aunque nadie abra la app.
builder.Services.AddHostedService<VerificacionVidaBackgroundService>();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// --- Documentacion interactiva: Swagger / OpenAPI con Swashbuckle ---
// AddOpenApi() (arriba) solo genera el DOCUMENTO json crudo con la
// descripcion de la Api (util para que otras herramientas lo consuman), pero
// no ofrece ninguna PANTALLA navegable para un humano. Swashbuckle agrega
// esa capa de UI (Swagger UI): una pagina web donde cualquiera (un dev de
// frontend, o el propio evaluador del proyecto) puede ver TODOS los
// endpoints documentados, sus parametros, sus posibles respuestas (200, 400,
// 401, etc.) y hasta EJECUTARLOS de prueba directamente desde el navegador,
// sin necesitar Postman ni escribir una sola linea de codigo cliente.
builder.Services.AddSwaggerGen(options =>
{
    // SecurityDefinition registra el ESQUEMA de autenticacion que Swagger UI
    // va a ofrecer: un boton "Authorize" donde pegar el Token JWT (obtenido
    // previamente desde POST /api/auth/login). Sin este bloque, Swagger UI no
    // sabria como enviar el header "Authorization: Bearer <token>" en los
    // requests de prueba hacia los endpoints protegidos con [Authorize].
    //
    // NOTA de version: Swashbuckle 10.x (la version compatible con .NET 10)
    // usa la libreria Microsoft.OpenApi 2.x, que reestructuro por completo su
    // API respecto a versiones anteriores (elimino el sub-namespace
    // ".Models", y reemplazo el patron "objeto + propiedad Reference" por
    // clases "...Reference" dedicadas, ej: OpenApiSecuritySchemeReference).
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pegar UNICAMENTE el token JWT (sin el prefijo 'Bearer '), obtenido de POST /api/auth/login."
    });

    // SecurityRequirement aplica ese esquema "Bearer" a TODOS los endpoints
    // documentados por defecto: asi, el boton "Authorize" de Swagger UI
    // queda disponible globalmente, sin tener que marcar endpoint por
    // endpoint cuales lo necesitan. En esta version de Microsoft.OpenApi, la
    // referencia al esquema "Bearer" ya definido arriba se arma con
    // OpenApiSecuritySchemeReference (recibe el "id" usado en
    // AddSecurityDefinition, y el OpenApiDocument "host" que Swashbuckle
    // provee via el parametro del lambda).
    options.AddSecurityRequirement(document =>
    {
        var requerimiento = new OpenApiSecurityRequirement();
        var referenciaEsquemaBearer = new OpenApiSecuritySchemeReference("Bearer", document, null);
        requerimiento.Add(referenciaEsquemaBearer, []);
        return requerimiento;
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // UseSwagger() expone el documento OpenAPI generado por Swashbuckle (un
    // JSON en "/swagger/v1/swagger.json"); UseSwaggerUI() sirve la PAGINA WEB
    // interactiva (en "/swagger") que lee ese JSON y arma la interfaz
    // navegable. Se dejan ambos solo en Development (igual que MapOpenApi
    // arriba): no tiene sentido exponer esta documentacion detallada de la
    // Api en un entorno de produccion real.
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// UseCors() debe ir ANTES de UseAuthentication()/UseAuthorization(): el
// navegador primero necesita saber (via los headers de respuesta que agrega
// este middleware) que el origen esta permitido, independientemente de si el
// request despues resulta autenticado o no.
app.UseCors(politicaCors);

// --- Orden del middleware: CRITICO ---
// UseAuthentication() debe ejecutarse SIEMPRE antes que UseAuthorization().
// Authentication responde la pregunta "¿quien sos?": lee el header
// "Authorization", valida el JWT contra las reglas configuradas arriba, y
// arma el ClaimsPrincipal ("HttpContext.User") que despues van a leer los
// controllers (ej: User.FindFirst(ClaimTypes.NameIdentifier) en
// ActivosDigitalesController). Authorization responde la pregunta "¿tenes
// permiso para ESTO?": evalua los atributos [Authorize] de cada endpoint
// contra ese User ya identificado. Si se invirtiera el orden, Authorization
// se ejecutaria sin saber todavia quien es el usuario, y [Authorize] nunca
// podria dejar pasar ningun request (incluso con un token perfectamente
// valido).
app.UseAuthentication();
app.UseAuthorization();

// Habilita el enrutamiento hacia los Controllers registrados arriba, en base
// a los atributos [Route]/[HttpGet]/[HttpPost]/etc. de cada uno.
app.MapControllers();

// Aplicar migraciones automáticamente al iniciar la app
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();
}

app.Run();
