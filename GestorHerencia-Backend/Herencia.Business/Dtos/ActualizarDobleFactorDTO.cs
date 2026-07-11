namespace Herencia.Business.Dtos;

// ActualizarDobleFactorDTO: body de PUT /api/usuarios/{id}/doble-factor.
// Un unico campo booleano: activar o desactivar el 2FA por email para la
// PROPIA cuenta autenticada (ver la verificacion de ownership en
// UsuariosController, identica a CambiarPassword/Actualizar).
public class ActualizarDobleFactorDTO
{
    public bool Habilitado { get; set; }
}
