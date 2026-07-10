using Herencia.Data.Models;

namespace Herencia.Business.Interfaces;

// INotificationService abstrae el ENVIO de una notificacion a un Usuario por
// el canal que haya elegido (ver MetodoNotificacion). La usan internamente
// VerificacionVidaService (recordatorios al titular, aviso al contacto de
// confianza) y CertificadoDefuncionService (pedido de certificado a los
// herederos, aviso de aprobacion/rechazo): ninguno de los dos sabe (ni le
// importa) COMO viaja realmente el mensaje.
//
// --- ¿Por que una interfaz propia, en vez de repetir el patron "notificacion
// simulada por consola" que ya usa ActivosDigitalesController? ---
// Ese patron esta hardcodeado directo en el controller porque, en ese caso,
// hay un UNICO canal posible (email simulado). Aca el titular elige entre
// TRES canales (Push/Email/SMS) y el mismo mensaje puede terminar
// disparandose desde DOS lugares distintos (el job de background y el
// controller de certificados), asi que vale la pena una abstraccion real:
// el dia que se integren proveedores de verdad (SMTP/SendGrid, Expo Push,
// Twilio), la UNICA clase a reemplazar es la implementacion de esta
// interfaz (ver NotificacionSimuladaService), sin tocar ninguna regla de
// negocio de los servicios que la consumen.
public interface INotificationService
{
    Task EnviarNotificacionAsync(Usuario destinatario, MetodoNotificacion canal, string asunto, string cuerpo);
}
