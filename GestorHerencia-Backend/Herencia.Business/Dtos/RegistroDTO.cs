namespace Herencia.Business.Dtos;

// Hereda de UsuarioCreacionDTO porque hoy pide los mismos datos que el alta
// administrativa, pero se mantiene como clase propia porque el auto-registro
// publico y el alta de administrador son casos de uso que podrian divergir
// (ej: requisitos o permisos distintos) sin avisar.
public class RegistroDTO : UsuarioCreacionDTO
{
}
