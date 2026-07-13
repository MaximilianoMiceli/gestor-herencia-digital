namespace Herencia.Data.Models;

/// <summary>Estado del flujo de aceptacion de una asignacion puntual (AsignacionHerencia.Estado).</summary>
// Arranca en 1 (no en 0) para distinguir un "0" mal migrado/insertado de un "Pendiente" real.
public enum EstadoBeneficiario
{
    Pendiente = 1,

    Aceptado = 2,

    // Estado final: AsignacionHerenciaService.CambiarEstadoAsync impide salir de Aceptado/Rechazado.
    Rechazado = 3
}
