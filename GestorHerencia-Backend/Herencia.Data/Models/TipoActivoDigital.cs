namespace Herencia.Data.Models;

// Enum que clasifica el tipo de activo digital a heredar.
// Usar un enum (en vez de un string libre) evita datos inconsistentes
// (ej: "Cripto" vs "cripto" vs "Criptomoneda") y EF Core lo persiste como INTEGER en SQLite.
public enum TipoActivoDigital
{
    CuentaBancaria = 0,
    RedSocial = 1,
    BilleteraCripto = 2,
    CorreoElectronico = 3,
    Otro = 4
}
