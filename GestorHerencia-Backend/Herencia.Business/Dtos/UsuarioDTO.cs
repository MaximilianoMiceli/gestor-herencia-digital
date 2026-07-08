using Herencia.Data.Models;

namespace Herencia.Business.Dtos;

// UsuarioDTO es la forma en la que los datos de un Usuario SALEN de la capa
// Business hacia afuera (en una etapa futura, hacia un controller de la capa
// Api que arma la respuesta JSON de un GET /api/usuarios/5).
//
// La diferencia clave con la entidad "Usuario" de Herencia.Data.Models es que
// este DTO deliberadamente NO incluye "PasswordHash" ni "PasswordSalt": esos
// dos campos son informacion sensible de seguridad que jamas debe salir de la
// capa Data/Business hacia un cliente externo, ni siquiera en forma de bytes
// "hasheados". Si algun dia expusieramos la entidad Usuario completa por error,
// estariamos filtrando esa informacion sensible en cualquier respuesta JSON.
// Con este DTO de salida, el "contrato" publico queda controlado explicitamente:
// solo viaja lo que es seguro y util para quien consume la Api.
public class UsuarioDTO
{
    // Id autogenerado por la base de datos. Se necesita en el DTO de salida para
    // que el cliente pueda usarlo despues en operaciones posteriores (ej: pedir
    // los ActivosDigitales de este usuario, actualizarlo, eliminarlo, etc.).
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    // Fecha de creacion del registro (dato de auditoria), util para mostrarla en
    // una futura UI (ej: "Usuario registrado el 07/07/2026").
    public DateTime FechaCreacion { get; set; }

    // Nivel de permisos del usuario. Se expone en el DTO de salida (a
    // diferencia de PasswordHash/PasswordSalt) porque NO es informacion
    // sensible: el frontend lo necesita, por ejemplo, para decidir si
    // mostrar o no una seccion administrativa en la UI.
    public RolUsuario Rol { get; set; }
}
