using Herencia.Business.Dtos;
using Herencia.Data.Models;

namespace Herencia.Business.Interfaces;

// IBeneficiarioService es el CONTRATO publico de la logica de negocio de
// Beneficiario, con exactamente el mismo criterio que IActivoDigitalService:
// trabaja solo con DTOs, y el UsuarioId titular debe existir antes de poder
// crear un Beneficiario asociado a el.
public interface IBeneficiarioService
{
    // Da de alta un nuevo Beneficiario para un Usuario titular puntual.
    // Puede lanzar:
    //  - ReglaNegocioException: si el Nombre/Email vienen vacios, o si ocurre
    //    un error tecnico al persistirlo.
    //  - RecursoNoEncontradoException: si el UsuarioId indicado no existe.
    Task<BeneficiarioDTO> CrearBeneficiarioAsync(BeneficiarioCreacionDTO beneficiarioCreacionDTO);

    // Busca un unico Beneficiario por su Id.
    Task<BeneficiarioDTO> ObtenerBeneficiarioPorIdAsync(int id);

    // Devuelve todos los Beneficiarios que pertenecen a un Usuario puntual.
    Task<IEnumerable<BeneficiarioDTO>> ObtenerBeneficiariosPorUsuarioAsync(int usuarioId);

    // Actualiza Nombre, Email y Parentesco de un Beneficiario existente.
    Task<BeneficiarioDTO> ActualizarBeneficiarioAsync(int id, BeneficiarioActualizacionDTO beneficiarioActualizacionDTO);

    // Elimina un Beneficiario existente.
    Task EliminarBeneficiarioAsync(int id);

    // CambiarEstadoAsync: implementa el flujo de ACEPTACION/RECHAZO de la
    // herencia digital. El beneficiario (o, en el estado actual de la Api,
    // el usuario titular en su nombre - ver la nota de ownership en
    // BeneficiariosController) usa este metodo para pasar su designacion de
    // "Pendiente" a un estado FINAL: "Aceptado" o "Rechazado".
    //
    // Puede lanzar:
    //  - RecursoNoEncontradoException: si el beneficiarioId no existe.
    //  - ReglaNegocioException: si el estado actual del beneficiario YA es
    //    "Aceptado" o "Rechazado" (una decision final no puede revisarse ni
    //    revertirse), o si "nuevoEstado" es "Pendiente" (no tiene sentido
    //    "volver atras" a un estado que solo el sistema asigna automaticamente
    //    al crear el beneficiario).
    Task<BeneficiarioDTO> CambiarEstadoAsync(int beneficiarioId, EstadoBeneficiario nuevoEstado);
}
