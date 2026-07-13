namespace Herencia.Data.Models;

/// <summary>
/// Canal por el que el titular recibe los recordatorios de verificacion de vida.
/// Mismo orden que "opcionesMetodo" en verificacion-vida.tsx (frontend) para un
/// mapeo directo entre indice de UI y enum.
/// </summary>
public enum MetodoNotificacion
{
    Push = 1,
    Email = 2,
    Sms = 3
}
