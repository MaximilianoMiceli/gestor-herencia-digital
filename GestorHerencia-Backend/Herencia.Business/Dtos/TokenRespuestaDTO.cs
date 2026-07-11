namespace Herencia.Business.Dtos;

// TokenRespuestaDTO envuelve el JWT emitido por ITokenService en una clase
// tipada (en vez de devolver un objeto anonimo "new { token = ... }" desde el
// controller), para que la respuesta de POST /api/auth/login tenga una forma
// (shape) explicita y documentable, coherente con el resto de los DTOs de
// salida de la Api.
public class TokenRespuestaDTO
{
    // Vacio cuando "RequiereDobleFactor" es true: en ese caso todavia NO se
    // emitio ningun JWT (ver AuthController.Login), y el cliente debe llamar
    // primero a POST /api/auth/verificar-doble-factor con el codigo recibido
    // por email para obtener recien ahi un Token real.
    public string Token { get; set; } = string.Empty;

    // true cuando el usuario tiene 2FA habilitado y el login (contraseña
    // correcta) todavia necesita el segundo factor antes de poder emitir un
    // JWT. El cliente debe mostrar la pantalla de "ingresar codigo" en vez de
    // considerar la sesion iniciada.
    public bool RequiereDobleFactor { get; set; }

    // Id del usuario cuyo login quedo pendiente de 2FA: el cliente lo
    // necesita para completar el body de POST /api/auth/verificar-doble-factor
    // (ese endpoint es publico, sin JWT todavia, asi que no hay otra forma de
    // identificar de quien es el codigo que se esta verificando).
    public int? UsuarioId { get; set; }
}
