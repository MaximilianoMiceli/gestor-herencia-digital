using System.ComponentModel.DataAnnotations;

namespace Herencia.Business.Dtos;

// LoginDTO es el "contrato" de entrada para POST /api/auth/login. Es
// deliberadamente MINIMO: solo pide las dos credenciales que el usuario
// realmente conoce de memoria (Email + Password en texto plano, transitorio),
// nunca su Id de base de datos ni ningun otro dato interno.
public class LoginDTO
{
    [Required(ErrorMessage = "El email es obligatorio.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = string.Empty;
}
