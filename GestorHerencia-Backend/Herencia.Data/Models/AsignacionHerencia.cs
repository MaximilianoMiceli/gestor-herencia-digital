namespace Herencia.Data.Models;

// Tabla intermedia (entidad de asociacion) que resuelve la relacion N-N
// entre ActivoDigital y Beneficiario.
// La modelamos como una clase explicita (en vez de dejar que EF cree una tabla
// intermedia "oculta") porque necesitamos guardar datos PROPIOS de la relacion:
// el porcentaje asignado y la condicion de liberacion. Eso no cabria en una
// tabla puente automatica de solo 2 FKs.
public class AsignacionHerencia : EntidadBaseAuditable
{
    public int Id { get; set; }

    // --- FK hacia ActivoDigital (lado N-N, parte 1) ---
    public int ActivoDigitalId { get; set; }

    public ActivoDigital ActivoDigital { get; set; } = null!;

    // --- FK hacia Beneficiario (lado N-N, parte 2) ---
    public int BeneficiarioId { get; set; }

    public Beneficiario Beneficiario { get; set; } = null!;

    // Porcentaje del activo que le corresponde a este beneficiario (0 a 100).
    // Se usa decimal (no float/double) porque es un valor monetario/proporcional
    // donde la precision exacta importa (evita errores de redondeo binario).
    public decimal PorcentajeAsignado { get; set; }

    // Condicion que debe cumplirse para liberar el activo hacia el beneficiario
    // (ej: "Certificado de defuncion + 30 dias sin objeciones", "Mayoria de edad").
    public string CondicionLiberacion { get; set; } = string.Empty;
}
