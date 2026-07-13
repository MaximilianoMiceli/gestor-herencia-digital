using Herencia.Business.Interfaces;

namespace Herencia.Api.Jobs;

/// <summary>
/// Ejecuta, en un ciclo periodico, IVerificacionVidaService.EjecutarEscaneoAsync() para
/// detectar titulares vencidos y disparar recordatorios/escalamiento sin depender de que
/// alguien abra la app.
/// </summary>
// Se inyecta IServiceScopeFactory (no el servicio directo) porque este BackgroundService es
// Singleton y el servicio es Scoped: se abre un scope nuevo en cada tick para evitar una "captive dependency".
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

        // Primer escaneo apenas arranca el proceso (no tiene sentido esperar 24hs para el
        // primero), y despues uno por cada tick del PeriodicTimer.
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
            using var scope = _scopeFactory.CreateScope();
            var verificacionVidaService = scope.ServiceProvider.GetRequiredService<IVerificacionVidaService>();

            await verificacionVidaService.EjecutarEscaneoAsync();
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            // Un error puntual en un escaneo no debe tumbar el proceso: se registra y se
            // reintenta en el proximo tick.
            _logger.LogError(ex, "Error inesperado durante el escaneo de verificacion de vida.");
        }
    }
}
