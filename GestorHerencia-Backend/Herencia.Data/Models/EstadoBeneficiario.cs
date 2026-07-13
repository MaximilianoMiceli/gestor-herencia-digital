namespace Herencia.Data.Models;

/// <summary>
/// Estado del flujo de aceptacion de UNA asignacion puntual (AsignacionHerencia.Estado):
/// es un atributo de esa fila, no de la persona beneficiaria en abstracto.
/// </summary>
// Enum en vez de bool/string libre: un bool no alcanza para 3 estados con
// semantica distinta ("no decidido" vs "decidio que no"), y un string libre
// permite inconsistencias de tipeo/mayusculas que un enum evita en tiempo de
// compilacion. Se persiste como INTEGER (default de EF Core), igual que el resto
// de los enums del proyecto. Arranca en 1 (no en 0) para poder distinguir una
// fila con el entero crudo "0" (dato mal migrado/insertado) de un "Pendiente" real.
public enum EstadoBeneficiario
{
    /// <summary>Estado inicial al registrar al beneficiario: todavia no respondio.</summary>
    Pendiente = 1,

    /// <summary>El beneficiario confirmo la designacion.</summary>
    Aceptado = 2,

    // Decision final, igual que Aceptado: AsignacionHerenciaService.CambiarEstadoAsync
    // impide pasar de Aceptado/Rechazado a cualquier otro estado.

    /// <summary>El beneficiario rechazo la designacion.</summary>
    Rechazado = 3
}
