using Herencia.Data.Models;

namespace Herencia.Business.Interfaces;

/// <summary>
/// Abstrae el envio de una notificacion a un Usuario por el canal elegido (ver
/// <see cref="MetodoNotificacion"/>), para poder reemplazar la implementacion simulada
/// por proveedores reales (SMTP/SendGrid, Push, Twilio) sin tocar a quienes la consumen.
/// </summary>
public interface INotificationService
{
    /// <summary>Envia "cuerpo" a "destinatario" por el canal indicado.</summary>
    Task EnviarNotificacionAsync(Usuario destinatario, MetodoNotificacion canal, string asunto, string cuerpo);
}
