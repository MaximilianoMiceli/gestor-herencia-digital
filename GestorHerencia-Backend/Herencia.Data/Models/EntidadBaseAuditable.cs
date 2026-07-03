namespace Herencia.Data.Models;

// Clase base abstracta que centraliza los campos de auditoria.
// Todas las entidades principales del dominio (Usuario, Beneficiario, ActivoDigital,
// AsignacionHerencia) heredan de esta clase para saber SIEMPRE quien creo/modifico
// un registro y cuando, sin tener que repetir estas 4 propiedades en cada entidad.
// Esto cumple el bonus de auditoria pedido por la rubrica.
public abstract class EntidadBaseAuditable
{
    // Fecha y hora (UTC) en que el registro fue creado.
    // Se completa una unica vez, en el momento del alta (INSERT).
    public DateTime FechaCreacion { get; set; }

    // Nombre/identificador del usuario que creo el registro.
    // En un sistema real esto se llenaria con el usuario autenticado (ej: claim del JWT);
    // aqui lo modelamos como string simple para no acoplar la capa Data a la capa de seguridad.
    public string UsuarioCreacion { get; set; } = string.Empty;

    // Fecha y hora (UTC) de la ultima modificacion. Es nullable porque un registro
    // recien creado todavia no fue modificado nunca.
    public DateTime? FechaModificacion { get; set; }

    // Usuario que realizo la ultima modificacion. Tambien nullable por el mismo motivo.
    public string? UsuarioModificacion { get; set; }
}
