namespace Herencia.Business.Dtos;

// TokenRespuestaDTO envuelve el JWT emitido por ITokenService en una clase
// tipada (en vez de devolver un objeto anonimo "new { token = ... }" desde el
// controller), para que la respuesta de POST /api/auth/login tenga una forma
// (shape) explicita y documentable, coherente con el resto de los DTOs de
// salida de la Api.
public class TokenRespuestaDTO
{
    public string Token { get; set; } = string.Empty;
}
