using System.ComponentModel.DataAnnotations;

namespace Herencia.Business.Dtos;

// UsuarioCreacionDTO ("Data Transfer Object") es la forma en la que los datos
// de un nuevo Usuario ENTRAN a la capa Business desde afuera (en una etapa
// futura, desde un controller de la capa Api que recibe un POST /api/usuarios).
//
// Por que no recibir directamente la entidad "Usuario" (la de Herencia.Data.Models)?
// 1) Seguridad / Sobre-posteo (Overposting): la entidad Usuario tiene propiedades
//    como "PasswordHash", "PasswordSalt", "FechaCreacion" o "Id" que NUNCA deberian
//    poder ser establecidas directamente por quien llama a la API. Si expusieramos
//    la entidad tal cual, un cliente malicioso podria enviar un JSON con esos
//    campos y manipularlos (ej: mandar su propio PasswordHash ya "hasheado" para
//    saltarse el proceso de seguridad, o falsificar la FechaCreacion). Al usar un
//    DTO, el "contrato" publico solo expone los campos que realmente tiene sentido
//    que el cliente envie: Nombre, Email y Password (en texto plano, de forma
//    transitoria, solo para que el servicio calcule el Hash+Salt).
// 2) Desacoplamiento: si el dia de manana la entidad Usuario cambia (se agrega o
//    renombra una columna en la base de datos), el "contrato" publico de la API
//    (este DTO) no tiene por que cambiar, y viceversa. Esto evita que un cambio
//    interno de la capa Data rompa a los clientes de la Api.
// 3) Validacion clara: al ser una clase chica y especifica para "creacion", es
//    mas facil documentar y validar exactamente que datos son obligatorios en
//    ESE momento puntual (alta de usuario), sin arrastrar propiedades de otras
//    etapas del ciclo de vida de un Usuario.
public class UsuarioCreacionDTO
{
    // Nombre completo del futuro titular de la cuenta. [Required] dispara un 400
    // automatico (via [ApiController]) si viene null/vacio, ANTES de llegar al
    // controller. El servicio, ademas, valida que no sea solo espacios en blanco.
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    // Email del usuario. [Required] cubre el caso "vacio"; el FORMATO (que tenga
    // arroba, dominio, etc.) se sigue validando en el servicio, que es quien
    // conoce la regla de negocio exacta (la regex usada).
    [Required(ErrorMessage = "El email es obligatorio.")]
    public string Email { get; set; } = string.Empty;

    // Contrasena en TEXTO PLANO, tal como la escribio el usuario en el formulario.
    // Esta propiedad es intencionalmente efimera: existe unicamente durante el
    // trayecto DTO -> UsuarioService, donde se usa para calcular PasswordHash y
    // PasswordSalt. Jamas se persiste este valor en la base de datos, y jamas
    // deberia loguearse ni devolverse en una respuesta de la Api.
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = string.Empty;

    // DNI del futuro titular. [Required] cubre el caso "vacio"; el FORMATO
    // (solo digitos, largo 7-8) se valida en el servicio, igual criterio que
    // el Email de arriba.
    [Required(ErrorMessage = "El DNI es obligatorio.")]
    public string Dni { get; set; } = string.Empty;

    // Fecha de nacimiento. Ademas de ser un dato de identidad, el servicio la
    // usa para exigir que el titular sea mayor de edad (ver
    // UsuarioService.CrearUsuarioAsync): un sistema que decide sobre la
    // liberacion de bienes hacia terceros no puede operar con menores.
    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
    public DateTime FechaNacimiento { get; set; }
}
