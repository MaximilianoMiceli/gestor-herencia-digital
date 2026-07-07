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
    // Nombre completo del futuro titular de la cuenta. Se valida en el servicio
    // que no venga vacio ni compuesto solo por espacios en blanco.
    public string Nombre { get; set; } = string.Empty;

    // Email del usuario. Se valida en el servicio que tenga un formato de email
    // correcto antes de intentar persistirlo (evita basura en la base de datos).
    public string Email { get; set; } = string.Empty;

    // Contrasena en TEXTO PLANO, tal como la escribio el usuario en el formulario.
    // Esta propiedad es intencionalmente efimera: existe unicamente durante el
    // trayecto DTO -> UsuarioService, donde se usa para calcular PasswordHash y
    // PasswordSalt. Jamas se persiste este valor en la base de datos, y jamas
    // deberia loguearse ni devolverse en una respuesta de la Api.
    public string Password { get; set; } = string.Empty;
}
