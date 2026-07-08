using Herencia.Business.Interfaces;
using Herencia.Business.Services;
using Herencia.Data;
using Herencia.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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

// --- Registro de la capa Business: Servicios ---
// Igual criterio: se registran las implementaciones concretas contra sus
// interfaces (IUsuarioService, IActivoDigitalService). Los controllers de la
// capa Api SOLO van a pedir estas interfaces por constructor; el contenedor
// de DI es el unico responsable de saber que implementacion concreta les
// corresponde inyectar.
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IActivoDigitalService, ActivoDigitalService>();

// --- Registro de la capa Business: Servicios de seguridad ---
// ISeguridadService (hash/salt de contrasenas) y ITokenService (emision de
// JWT) son servicios UTILITARIOS PUROS: no dependen de ningun repositorio ni
// de AppDbContext, por lo que tecnicamente podrian registrarse incluso como
// Singleton. Se dejan como Scoped por consistencia con el resto de los
// servicios de la solucion (mismo ciclo de vida por request).
builder.Services.AddScoped<ISeguridadService, SeguridadService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// --- Registro de la capa Api: Controllers ---
// Habilita el uso de Controllers basados en atributos ([ApiController],
// [HttpGet], [Route], etc.), como UsuariosController y ActivosDigitalesController.
builder.Services.AddControllers();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Habilita el enrutamiento hacia los Controllers registrados arriba, en base
// a los atributos [Route]/[HttpGet]/[HttpPost]/etc. de cada uno.
app.MapControllers();

app.Run();
