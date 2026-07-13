namespace Herencia.Business.Dtos;

// Hereda de UsuarioCreacionDTO (mismos datos hoy), pero se mantiene aparte porque
// el auto-registro publico y el alta de administrador podrian divergir despues.
public class RegistroDTO : UsuarioCreacionDTO
{
}
