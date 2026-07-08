using System.Security.Cryptography;
using System.Text;
using Herencia.Business.Interfaces;

namespace Herencia.Business.Services;

// SeguridadService es la implementacion CONCRETA de ISeguridadService: contiene
// el algoritmo criptografico real usado para proteger las contrasenas de los
// Usuarios. Es un servicio UTILITARIO PURO: no inyecta ningun repositorio ni
// AppDbContext, solo trabaja con los bytes/strings que recibe por parametro y
// devuelve un resultado. Esto lo hace, ademas, muy facil de testear de forma
// unitaria (no requiere una base de datos real ni mocks complejos).
public class SeguridadService : ISeguridadService
{
    // CrearPasswordHash: calcula un hash + salt NUEVOS para una contrasena en
    // texto plano. Se usa cuando un Usuario se registra, o cuando cambia su
    // contrasena.
    public void CrearPasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        // --- ¿Por que HMACSHA512 y no un simple SHA512.ComputeHash(password)? ---
        // SHA512 "a secas" es una funcion de hash CRIPTOGRAFICA pero
        // DETERMINISTICA: la misma contrasena SIEMPRE produce el mismo hash.
        // Esto es un problema de seguridad grave, porque permite dos ataques
        // muy conocidos:
        //   a) "Rainbow tables": tablas precalculadas de millones de hashes de
        //      contrasenas comunes (ej: "123456", "password"). Si dos usuarios
        //      distintos usan la misma contrasena, ambos terminarian con el
        //      MISMO hash en la base de datos, y un atacante que robe la base
        //      de datos podria buscar ese hash en una rainbow table publica y
        //      recuperar la contrasena original en segundos.
        //   b) Ataques de diccionario offline: un atacante con la base de
        //      datos filtrada puede probar millones de contrasenas comunes,
        //      hashearlas con el mismo algoritmo, y comparar el resultado
        //      contra TODOS los usuarios de la tabla a la vez (un solo calculo
        //      de hash "rompe" potencialmente muchas cuentas simultaneamente).
        //
        // HMACSHA512 resuelve esto porque es un "Hash-based Message
        // Authentication Code": en vez de hashear SOLO la contrasena, combina
        // criptograficamente la contrasena CON UNA CLAVE SECRETA (el "Key" de
        // HMACSHA512). Si construimos HMACSHA512 SIN pasarle una clave por
        // parametro, la clase .NET genera automaticamente, por nosotros, una
        // clave ALEATORIA de 128 bytes (1024 bits) usando un generador
        // criptograficamente seguro (CSPRNG). Esa clave aleatoria ES el "SALT".
        using var hmac = new HMACSHA512();

        // --- ¿Que es el SALT y por que es distinto para cada usuario? ---
        // El Salt es, justamente, ese valor aleatorio unico (hmac.Key) generado
        // para ESTA contrasena en particular. Al guardarlo junto al hash (pero
        // NUNCA junto a la contrasena en texto plano), logramos que:
        //   - Dos usuarios con la MISMA contrasena en texto plano terminen con
        //     hashes COMPLETAMENTE DISTINTOS en la base de datos (porque cada
        //     uno tiene su propio salt aleatorio combinado en el calculo).
        //   - Las rainbow tables precalculadas dejan de servir: un atacante
        //     tendria que generar una tabla nueva Y DISTINTA para CADA salt
        //     individual, lo cual vuelve el ataque computacionalmente
        //     inviable a escala.
        // El salt NO necesita mantenerse secreto (se guarda en texto plano en
        // la base de datos, junto al hash): su valor de seguridad no viene de
        // ser secreto, sino de ser UNICO por usuario.
        passwordSalt = hmac.Key;

        // ComputeHash recibe la contrasena convertida a bytes (codificacion
        // UTF8) y calcula el HMAC resultante de combinarla criptograficamente
        // con la clave/salt configurada arriba. El resultado es un arreglo de
        // 64 bytes (512 bits): el "PasswordHash" que efectivamente se persiste.
        passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    }

    // VerificarPasswordHash: recalcula el hash de una contrasena candidata
    // (ej: la que un usuario tipeo al intentar loguearse) usando el MISMO salt
    // que se uso originalmente al crear la cuenta, y compara el resultado
    // contra el hash guardado en la base de datos.
    public bool VerificarPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
    {
        // Esta vez SI le pasamos una clave al constructor de HMACSHA512: el
        // "passwordSalt" que fue generado y persistido en el momento del
        // registro de este usuario puntual. Es indispensable usar EXACTAMENTE
        // el mismo salt que se uso para crear el hash original: HMAC con una
        // clave distinta produce, siempre, un resultado distinto, aunque la
        // contrasena en texto plano sea identica.
        using var hmac = new HMACSHA512(passwordSalt);

        // Recalculamos el hash de la contrasena CANDIDATA (la recien tipeada)
        // con ese mismo salt. Si el usuario escribio la contrasena correcta,
        // este calculo deberia dar exactamente el mismo resultado que el
        // "passwordHash" guardado en la base de datos.
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

        // --- ¿Por que NO comparamos con computedHash.SequenceEqual(passwordHash)? ---
        // Una comparacion "ingenua" byte a byte con SequenceEqual (o con un
        // simple bucle "for") se corta apenas encuentra el PRIMER byte
        // distinto. Esto significa que, en promedio, comparar un hash
        // CASI correcto tarda una fraccion de microsegundo MAS que comparar
        // uno completamente incorrecto, porque tuvo que revisar mas bytes
        // antes de fallar. Esa diferencia de tiempo, aunque minuscula, es
        // MEDIBLE por un atacante sofisticado que hace muchisimos intentos
        // remotos y analiza estadisticamente los tiempos de respuesta: esto se
        // conoce como "ataque de temporizacion" (timing attack), y en teoria
        // permitiria reconstruir el hash correcto byte por byte.
        //
        // CryptographicOperations.FixedTimeEquals (System.Security.Cryptography)
        // esta diseñado especificamente para comparar datos sensibles (hashes,
        // tokens, claves): SIEMPRE recorre TODOS los bytes de ambos arreglos,
        // sin importar en que posicion aparece la primera diferencia, de modo
        // que el tiempo de ejecucion es constante y no revela informacion
        // sobre CUANTOS bytes coincidieron. Esta es la forma "segura" de
        // comparar que pide la rubrica.
        return CryptographicOperations.FixedTimeEquals(computedHash, passwordHash);
    }
}
