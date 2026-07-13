namespace Herencia.Business.Dtos;

public class TokenRespuestaDTO
{
    // Vacio cuando "RequiereDobleFactor" es true: el JWT recien se emite
    // despues de POST /api/auth/verificar-doble-factor.
    public string Token { get; set; } = string.Empty;

    public bool RequiereDobleFactor { get; set; }

    // El cliente lo necesita para completar el body de verificar-doble-factor
    // (endpoint publico, sin JWT todavia).
    public int? UsuarioId { get; set; }
}
