using Herencia.Business.Interfaces;
using Herencia.Data.Models;
using Microsoft.Extensions.Logging;

namespace Herencia.Business.Services;

// NotificacionSimuladaService es la UNICA implementacion de
// INotificationService por ahora: en vez de integrar proveedores reales
// (SMTP/SendGrid para Email, Expo Push Api para Push, Twilio para SMS, que
// requieren credenciales y coordinacion con el frontend fuera del alcance
// de esta etapa), deja constancia por consola de que la notificacion
// "hubiera salido", con el mismo criterio ya usado en
// ActivosDigitalesController para la invitacion de beneficiarios y en
// UsuarioService para el reset de password ("simulado, por consola").
public class NotificacionSimuladaService : INotificationService
{
    private readonly ILogger<NotificacionSimuladaService> _logger;

    public NotificacionSimuladaService(ILogger<NotificacionSimuladaService> logger)
    {
        _logger = logger;
    }

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

        // Ademas del Console.WriteLine (pensado para verse en vivo durante
        // una demo), se deja tambien el registro en el ILogger estandar:
        // es lo que permitiria, el dia de mañana, auditar en un archivo de
        // log cuantas notificaciones se dispararon sin depender de tener la
        // consola abierta.
        _logger.LogInformation(
            "Notificacion simulada enviada por {Canal} a {Email}: {Asunto}",
            canal, destinatario.Email, asunto);

        return Task.CompletedTask;
    }
}
