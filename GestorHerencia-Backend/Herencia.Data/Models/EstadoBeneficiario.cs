namespace Herencia.Data.Models;

// EstadoBeneficiario representa en que punto del "flujo de aceptacion" se
// encuentra UNA asignacion puntual (AsignacionHerencia.Estado) dentro del
// proceso de herencia digital: es el estado del Usuario que actua como
// BENEFICIARIO en esa fila especifica, no un atributo global de la persona
// (que podria tener otras asignaciones en estados distintos).
//
// Por que un ENUM y no un simple booleano ("Aceptado: true/false") o un string
// libre ("pendiente"/"Pendiente"/"ACEPTADO")?
// 1) Un booleano solo puede representar DOS estados, y aca necesitamos TRES
//    (Pendiente, Aceptado, Rechazado) con semantica bien distinta cada uno:
//    "todavia no se decidio" NO es lo mismo que "se decidio que no". Forzar
//    eso a un bool obligaria a un segundo campo auxiliar (ej. "FueRechazado")
//    para no perder informacion, duplicando el estado en dos columnas que
//    podrian volverse inconsistentes entre si.
// 2) Un string libre permite errores de tipeo o de mayusculas/minusculas
//    ("Pendiente" vs "pendiente" vs "PENDIENTE") que silenciosamente rompen
//    cualquier comparacion o filtro (ej. "WHERE Estado = 'Pendiente'" no
//    encontraria las filas guardadas como "pendiente"). Con un enum, el
//    COMPILADOR impide asignar cualquier valor que no sea uno de los tres
//    definidos: es imposible, en tiempo de compilacion, dejar un Beneficiario
//    en un estado que no exista.
// 3) EF Core persiste un enum como INTEGER por defecto (igual que ya se hace
//    con RolUsuario y TipoActivoDigital en este mismo proyecto), lo cual es
//    mas compacto y mas rapido de indexar/comparar en la base de datos que
//    un string, sin perder legibilidad del lado del codigo C#.
//
// Los valores arrancan en 1 (no en el 0 por defecto de C#) A PROPOSITO: asi,
// si alguna vez una fila queda con el entero crudo "0" en la base de datos
// (por ejemplo, por una migracion de datos externa mal hecha, o un INSERT
// manual que se olvido de completar la columna), ese valor NO matchea con
// ningun miembro valido del enum y el problema se detecta rapido, en vez de
// confundirse silenciosamente con "Pendiente".
public enum EstadoBeneficiario
{
    // Estado inicial: se asigna automaticamente cuando el usuario titular
    // registra al beneficiario (POST /api/beneficiarios). Significa "todavia
    // no se le pregunto, o todavia no respondio".
    Pendiente = 1,

    // El beneficiario confirmo que acepta ser designado como tal.
    Aceptado = 2,

    // El beneficiario rechazo la designacion. Es una decision FINAL, igual
    // que Aceptado: ver la regla de negocio en AsignacionHerenciaService.CambiarEstadoAsync,
    // que impide pasar de Aceptado/Rechazado a cualquier otro estado.
    Rechazado = 3
}
