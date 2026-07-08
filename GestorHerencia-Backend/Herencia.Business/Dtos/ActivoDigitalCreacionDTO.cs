using System.ComponentModel.DataAnnotations;
using Herencia.Data.Models;

namespace Herencia.Business.Dtos;

// ActivoDigitalCreacionDTO define el "contrato" de entrada para dar de alta un
// nuevo ActivoDigital. Igual que con UsuarioCreacionDTO, evitamos exponer la
// entidad "ActivoDigital" (Herencia.Data.Models) directamente porque esa
// entidad tiene propiedades que NO deberian ser completadas a mano por quien
// llama a la Api, como "Id" (lo genera la base de datos), las propiedades de
// auditoria heredadas de EntidadBaseAuditable (FechaCreacion, UsuarioCreacion,
// etc., que las completa el propio sistema) o la coleccion de navegacion
// "AsignacionesHerencia" (que pertenece a otra etapa del dominio, la de
// repartir el activo entre beneficiarios).
public class ActivoDigitalCreacionDTO
{
    // Nombre descriptivo del activo (ej: "Cuenta Banco Santander"). [Required]
    // dispara un 400 automatico si viene null/vacio; el servicio ademas valida
    // que no sea solo espacios en blanco.
    [Required(ErrorMessage = "El nombre del activo es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    // Categoria del activo. Al reutilizar el mismo enum "TipoActivoDigital" que
    // usa la entidad de Data, evitamos duplicar esta clasificacion en dos
    // lugares distintos (el enum en si no es un detalle de infraestructura de
    // base de datos, es parte del vocabulario del dominio, por eso es seguro
    // reutilizarlo aca sin romper el desacople entre capas).
    public TipoActivoDigital Tipo { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    // Id del Usuario titular al que se le va a asociar este nuevo activo.
    // ActivoDigitalService valida, ANTES de crear el activo, que este Usuario
    // realmente exista (regla de negocio pedida explicitamente por la rubrica).
    public int UsuarioId { get; set; }
}
