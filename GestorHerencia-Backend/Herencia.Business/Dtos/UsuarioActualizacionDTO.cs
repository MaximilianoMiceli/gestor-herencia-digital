using System.ComponentModel.DataAnnotations;

namespace Herencia.Business.Dtos;

// UsuarioActualizacionDTO es el "contrato" de entrada para un PUT /api/usuarios/{id}.
// Es una clase separada de UsuarioCreacionDTO (y no la reutilizamos tal cual) por dos
// motivos:
// 1) La actualizacion NO incluye "Password": cambiar la contrasena es una operacion
//    sensible que, en un sistema real, ameritaria su propio endpoint dedicado (con
//    verificacion de la contrasena actual, por ejemplo). Mezclarla aca abrira la
//    puerta a que un PUT "de edicion de datos basicos" termine, sin querer,
//    reseteando las credenciales de otro usuario.
// 2) Semantica: este DTO documenta explicitamente que, al ACTUALIZAR un Usuario,
//    solo tiene sentido de negocio modificar su Nombre y su Email.
//
// [Required] son Data Annotations: ASP.NET Core, gracias al atributo [ApiController]
// del controller, valida automaticamente el ModelState ANTES de que el codigo del
// endpoint se ejecute. Si "Nombre" o "Email" llegan null/vacios, el framework
// devuelve un 400 Bad Request automatico, sin que el controller tenga que escribir
// ese chequeo a mano. Esto es una PRIMERA linea de defensa (validacion estructural);
// la validacion de NEGOCIO mas fina (formato de email, largo minimo, etc.) se sigue
// resolviendo en UsuarioService, que es quien conoce las reglas del dominio.
public class UsuarioActualizacionDTO
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio.")]
    public string Email { get; set; } = string.Empty;
}
