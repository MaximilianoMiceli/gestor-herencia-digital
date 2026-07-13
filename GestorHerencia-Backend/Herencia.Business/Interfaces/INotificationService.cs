using Herencia.Data.Models;

namespace Herencia.Business.Interfaces;

/// <summary>
/// Abstrae el envio de una notificacion a un Usuario por el canal elegido (ver
/// <see cref="MetodoNotificacion"/>). La usan VerificacionVidaService (recordatorios,
/// aviso al contacto de confianza) y CertificadoDefuncionService (pedido de certificado,
/// aviso de aprobacion/rechazo). A diferencia del email simulado hardcodeado en
/// ActivosDigitalesController (que solo tiene un canal posible), aca el titular elige
/// entre tres canales y el mismo mensaje puede dispararse desde mas de un lugar, por lo
/// que amerita una abstraccion real: el dia que se integren proveedores de verdad
/// (SMTP/SendGrid, Expo Push, Twilio), solo hay que reemplazar la implementacion (ver
/// NotificacionSimuladaService), sin tocar reglas de negocio de quienes la consumen.
/// </summary>
public interface INotificationService
{
    /// <summary>Envia "cuerpo" a "destinatario" por el canal indicado.</summary>
    Task EnviarNotificacionAsync(Usuario destinatario, MetodoNotificacion canal, string asunto, string cuerpo);
}
