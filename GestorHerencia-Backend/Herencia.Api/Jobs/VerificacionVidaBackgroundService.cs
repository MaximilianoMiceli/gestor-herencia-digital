using Herencia.Business.Interfaces;

namespace Herencia.Api.Jobs;

// VerificacionVidaBackgroundService ejecuta, en un ciclo periodico,
// IVerificacionVidaService.EjecutarEscaneoAsync() para detectar titulares
// vencidos y disparar recordatorios/escalamiento sin depender de que
// alguien abra la app.
//
// --- ¿Por que IServiceScopeFactory y no inyectar IVerificacionVidaService directo? ---
// Un BackgroundService se registra como SINGLETON (una unica instancia
// durante toda la vida del proceso), pero IVerificacionVidaService (y,
// transitivamente, AppDbContext) es SCOPED (una instancia nueva por
// request HTTP, ver Program.cs). Inyectar un servicio Scoped directo en el
// constructor de un Singleton es un error clasico de "captive dependency"
// que el propio contenedor de DI de ASP.NET Core rechaza en tiempo de
// ejecucion. La solucion estandar es pedir IServiceScopeFactory (que SI es
// Singleton) y abrir un scope nuevo, de corta vida, en cada tick: el mismo
// ciclo de vida que tendria una request HTTP normal.
public class VerificacionVidaBackgroundService : BackgroundService
{
    private static readonly TimeSpan IntervaloEntreEscaneos = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VerificacionVidaBackgroundService> _logger;

    public VerificacionVidaBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<VerificacionVidaBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(IntervaloEntreEscaneos);

        // Se ejecuta un primer escaneo apenas arranca el proceso (no tiene
        // sentido esperar 24hs para el primero), y despues uno por cada
        // tick del PeriodicTimer.
        do
        {
            await EjecutarUnEscaneoAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task EjecutarUnEscaneoAsync(CancellationToken stoppingToken)
    {
        try
        {
            // "using" (no "await using" con vida larga): el scope se crea y
            // se descarta DENTRO de este mismo tick, nunca se mantiene
            // abierto entre ejecuciones.
            using var scope = _scopeFactory.CreateScope();
            var verificacionVidaService = scope.ServiceProvider.GetRequiredService<IVerificacionVidaService>();

            await verificacionVidaService.EjecutarEscaneoAsync();
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            // Un error puntual en UN escaneo (ej: la base de datos no
            // respondio momentaneamente) no debe tumbar el proceso
            // completo: se registra y se reintenta en el proximo tick.
            _logger.LogError(ex, "Error inesperado durante el escaneo de verificacion de vida.");
        }
    }
}
