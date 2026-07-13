namespace Herencia.Data.Models;

/// <summary>Categoria de un activo digital.</summary>
// Enum en vez de string libre: evita datos inconsistentes (ej: "Cripto" vs
// "cripto") y EF Core lo persiste como INTEGER en SQLite.
public enum TipoActivoDigital
{
    CuentaBancaria = 0,
    RedSocial = 1,
    BilleteraCripto = 2,
    CorreoElectronico = 3,
    Otro = 4
}
