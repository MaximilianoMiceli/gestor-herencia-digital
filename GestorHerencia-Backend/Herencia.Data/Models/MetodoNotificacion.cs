namespace Herencia.Data.Models;

// MetodoNotificacion es el canal elegido por el titular para recibir los
// recordatorios de verificacion de vida. Mismo orden que "opcionesMetodo" en
// la pantalla verificacion-vida.tsx del frontend (Push, Email, SMS), para
// que un futuro mapeo entre el indice de la UI y este enum sea directo.
public enum MetodoNotificacion
{
    Push = 1,
    Email = 2,
    Sms = 3
}
