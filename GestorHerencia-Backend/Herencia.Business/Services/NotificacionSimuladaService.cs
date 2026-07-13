using Herencia.Business.Interfaces;
using Herencia.Data.Models;
using Microsoft.Extensions.Logging;

namespace Herencia.Business.Services;

/// <summary>
/// Única implementación de <see cref="INotificationService"/>. En vez de integrar
/// proveedores reales (SMTP/SendGrid, Push, Twilio para SMS — fuera del alcance de esta
/// etapa), deja constancia por consola de que la notificación "hubiera salido".
/// </summary>
public class NotificacionSimuladaService : INotificationService
{
    private readonly ILogger<NotificacionSimuladaService> _logger;

    public NotificacionSimuladaService(ILogger<NotificacionSimuladaService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Simula el envío de una notificación imprimiéndola por consola y registrándola
    /// en el logger estándar.
    /// </summary>
    public Task EnviarNotificacionAsync(Usuario destinatario, MetodoNotificacion canal, string asunto, string cuerpo)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine();
        Console.WriteLine("======================================================================");
        Console.WriteLine($"[NOTIFICACION SIMULADA - {canal}] Para: {destinatario.Nombre} <{destinatario.Email}>");
        Console.WriteLine($"Asunto: {asunto}");
        Console.WriteLine(cuerpo);
        Console.WriteLine("======================================================================");
        Console.WriteLine();
        Console.ResetColor();

        // Además del Console.WriteLine (para verse en vivo durante una demo), se registra
        // en el ILogger estándar para poder auditar sin depender de tener la consola abierta.
        _logger.LogInformation(
            "Notificacion simulada enviada por {Canal} a {Email}: {Asunto}",
            canal, destinatario.Email, asunto);

        return Task.CompletedTask;
    }
}
