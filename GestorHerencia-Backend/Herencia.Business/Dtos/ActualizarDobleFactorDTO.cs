namespace Herencia.Business.Dtos;

// Activa/desactiva el 2FA por email para la propia cuenta autenticada.
public class ActualizarDobleFactorDTO
{
    public bool Habilitado { get; set; }
}
