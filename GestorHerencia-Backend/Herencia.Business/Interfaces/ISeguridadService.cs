namespace Herencia.Business.Interfaces;

// ISeguridadService es el CONTRATO publico de todo lo relacionado a
// CRIPTOGRAFIA DE CONTRASEÑAS dentro de la capa Business. Se separa en su
// propia interfaz (en vez de dejar esta logica "enterrada" como metodo privado
// de UsuarioService, como estaba antes) por dos motivos:
// 1) Responsabilidad unica (S de SOLID): calcular/verificar un hash de
//    contrasena es una preocupacion de SEGURIDAD, independiente de las reglas
//    de negocio de "que es un Usuario valido". Separarla evita que ambas
//    responsabilidades queden mezcladas en una misma clase.
// 2) Reutilizacion: cualquier otro servicio que en el futuro necesite hashear
//    o verificar contrasenas (ej: un futuro flujo de "cambiar contrasena" o de
//    "recuperar cuenta") puede inyectar esta MISMA interfaz, en vez de duplicar
//    el algoritmo criptografico en mas de un lugar. Tener el algoritmo
//    duplicado en varias clases es, en si mismo, un riesgo de seguridad: si el
//    dia de mañana hay que migrar de algoritmo, hay que acordarse de
//    actualizar TODOS los lugares donde se copio la logica.
//
// Esta interfaz es un servicio UTILITARIO PURO: no depende de IUsuarioRepository,
// de AppDbContext, ni de ninguna otra pieza de infraestructura. Solo hace
// calculos criptograficos en memoria a partir de los parametros que recibe.
public interface ISeguridadService
{
    // Genera un par (hash, salt) NUEVO a partir de una contrasena en texto
    // plano. Se usan parametros "out" (en vez de devolver una tupla) porque es
    // la firma exacta pedida por la rubrica, y ademas dejar en claro que esta
    // operacion produce DOS salidas igual de importantes: no tiene sentido
    // persistir el hash sin el salt (no se podria volver a verificar la
    // contrasena), ni el salt sin el hash.
    void CrearPasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt);

    // Verifica si una contrasena en texto plano (ej: la que un usuario tipeo en
    // un formulario de login) corresponde al hash+salt guardados en la base de
    // datos para ese usuario. Devuelve simplemente true/false: quien llama a
    // este metodo (tipicamente un futuro servicio/endpoint de Login) decide que
    // hacer con el resultado (ej: emitir un Token JWT si es true, o rechazar el
    // login si es false).
    bool VerificarPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt);
}
