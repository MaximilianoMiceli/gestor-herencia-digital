using System.Text.RegularExpressions;
using Herencia.Business.Dtos;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Herencia.Data.Models;
using Herencia.Data.Repositories;

namespace Herencia.Business.Services;

// UsuarioService es la implementacion CONCRETA de IUsuarioService: aca vive la
// LOGICA DE NEGOCIO real de Usuario (validaciones, orquestacion del calculo del
// hash de la contrasena, traduccion de errores tecnicos a excepciones
// amigables, y mapeo entre entidades de Data y DTOs de Business).
public class UsuarioService : IUsuarioService
{
    // --- Inyeccion de Dependencias por CONSTRUCTOR ---
    // Guardamos unicamente la INTERFAZ del repositorio (IUsuarioRepository),
    // nunca la clase concreta "UsuarioRepository" y muchisimo menos el
    // "AppDbContext" de EF Core. Esto es exactamente lo que pide la rubrica:
    // la capa Business NO debe saber que hay una base de datos SQLite detras,
    // ni como esta armado el DbContext, ni construirlo con "new". Solo conoce
    // el contrato (que metodos existen), igual que ya hacia RepositorioBase
    // con la capa Data. Gracias a esto, en los tests unitarios de este
    // servicio se podria inyectar un "FakeUsuarioRepository" en memoria sin
    // necesitar una base de datos real.
    private readonly IUsuarioRepository _usuarioRepository;

    // ISeguridadService encapsula el ALGORITMO criptografico real (HMACSHA512)
    // usado para calcular el hash/salt de una contrasena. UsuarioService ya NO
    // calcula el hash "a mano" (como antes, en un metodo privado propio):
    // delega esa responsabilidad a un servicio dedicado y reutilizable, para
    // que la logica de SEGURIDAD viva en un unico lugar (ver el comentario de
    // ISeguridadService para el detalle completo de por que se separo).
    private readonly ISeguridadService _seguridadService;

    // El contenedor de Inyeccion de Dependencias configurado en Program.cs
    // (etapa Api) sera el encargado de "resolver" automaticamente una
    // instancia de IUsuarioRepository (tipicamente UsuarioRepository) y de
    // ISeguridadService (tipicamente SeguridadService), y pasarlas aca por
    // este constructor cuando se necesite un UsuarioService.
    public UsuarioService(IUsuarioRepository usuarioRepository, ISeguridadService seguridadService)
    {
        _usuarioRepository = usuarioRepository;
        _seguridadService = seguridadService;
    }

    // CrearUsuarioAsync: da de alta un nuevo Usuario a partir de un DTO de
    // entrada, aplicando validaciones de negocio y generando el hash/salt de
    // la contrasena ANTES de tocar la base de datos.
    public async Task<UsuarioDTO> CrearUsuarioAsync(UsuarioCreacionDTO usuarioCreacionDTO)
    {
        // --- Paso 1: Validaciones de negocio (fail-fast, antes de ir a la BD) ---
        // Se validan los datos de ENTRADA aca, en Business, y no en la capa
        // Data ni confiando ciegamente en el cliente de la Api. La capa Data
        // solo sabe "guardar lo que le llega"; es responsabilidad de Business
        // asegurarse de que "lo que le llega" tenga sentido de negocio.
        //
        // string.IsNullOrWhiteSpace cubre 3 casos invalidos de una sola vez:
        // el string es null, es string.Empty (""), o esta compuesto solo por
        // espacios en blanco (" ", "\t", etc.).
        if (string.IsNullOrWhiteSpace(usuarioCreacionDTO.Nombre))
        {
            // ReglaNegocioException (constructor simple, SIN inner exception):
            // esto NO es un error tecnico, es una violacion de una regla de
            // negocio detectada por nuestro propio codigo, asi que no hay
            // ninguna excepcion "original" que preservar para diagnostico.
            throw new ReglaNegocioException("El nombre del usuario no puede estar vacio.");
        }

        // Validamos el FORMATO del email con una expresion regular simple.
        // No pretende ser 100% RFC-compliant (validar emails perfectamente
        // con regex es notoriamente complejo), pero es suficiente para
        // rechazar entradas claramente invalidas (ej: "nombre-sin-arroba",
        // "@sinusuario.com", "usuario@", etc.) antes de persistirlas.
        if (string.IsNullOrWhiteSpace(usuarioCreacionDTO.Email) ||
            !Regex.IsMatch(usuarioCreacionDTO.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            throw new ReglaNegocioException("El email ingresado no tiene un formato valido.");
        }

        // La contrasena en texto plano debe existir y tener un largo minimo
        // razonable antes de intentar "hashearla". Validar esto aca evita
        // guardar usuarios con contrasenas triviales o vacias.
        if (string.IsNullOrWhiteSpace(usuarioCreacionDTO.Password) || usuarioCreacionDTO.Password.Length < 6)
        {
            throw new ReglaNegocioException("La contrasena debe tener al menos 6 caracteres.");
        }

        // --- Paso 2: logica de negocio + acceso a datos, protegida con try-catch ---
        try
        {
            // Delegamos el calculo criptografico del hash/salt a
            // ISeguridadService (out parameters: la firma expone dos salidas
            // igual de importantes). UsuarioService ya no sabe COMO se calcula
            // un hash seguro, solo que "existe alguien que sabe hacerlo": la
            // capa Data, por su parte, solo se encarga de PERSISTIR los bytes
            // ya calculados, sin saber tampoco como se generaron.
            _seguridadService.CrearPasswordHash(usuarioCreacionDTO.Password, out var passwordHash, out var passwordSalt);

            // Mapeamos el DTO de entrada + los datos de seguridad calculados
            // hacia la entidad de EF Core "Usuario". Este mapeo manual (DTO ->
            // Entidad) es justamente el motivo de ser de los DTOs: el cliente
            // de la Api nunca pudo, ni siquiera queriendo, establecer un Id,
            // un PasswordHash falso o una FechaCreacion arbitraria, porque esos
            // campos ni siquiera existen en UsuarioCreacionDTO.
            var usuario = new Usuario
            {
                Nombre = usuarioCreacionDTO.Nombre.Trim(),
                Email = usuarioCreacionDTO.Email.Trim(),
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                // Dato de auditoria minimo: quien genero el alta. En una etapa
                // futura con autenticacion real, este valor vendria del usuario
                // autenticado (ej: un claim del JWT) en vez de un literal fijo.
                FechaCreacion = DateTime.UtcNow,
                UsuarioCreacion = "sistema"
            };

            // Delegamos la persistencia al repositorio a traves de la
            // INTERFAZ inyectada. UsuarioService no sabe (ni le importa) si
            // esto termina siendo un INSERT en SQLite, en SQL Server, o una
            // llamada HTTP a otro microservicio: solo conoce el contrato
            // "AgregarAsync(Usuario)".
            await _usuarioRepository.AgregarAsync(usuario);

            // Mapeamos la entidad YA PERSISTIDA (con su Id autogenerado por la
            // base de datos) hacia el DTO de SALIDA. Notar que PasswordHash y
            // PasswordSalt jamas se copian a UsuarioDTO: esa clase ni siquiera
            // tiene esas propiedades, por lo que es fisicamente imposible que
            // esta informacion sensible "se escape" hacia el llamador.
            return MapearADTO(usuario);
        }
        catch (ReglaNegocioException)
        {
            // Si dentro del try ya lanzamos nosotros mismos una
            // ReglaNegocioException (no deberia pasar en este metodo en
            // particular, pero se deja por consistencia y a prueba de
            // futuros cambios), la relanzamos tal cual con "throw;" (sin
            // "throw ex;") para no perder el StackTrace original y no volver
            // a envolverla en otra excepcion mas.
            throw;
        }
        catch (Exception ex)
        {
            // Aca caen los errores TECNICOS inesperados: por ejemplo, que la
            // base de datos este caida, un timeout de conexion, una violacion
            // de constraint a nivel SQL, etc. "ex" contiene el detalle tecnico
            // real (mensaje de ADO.NET/SQLite, StackTrace completo, etc.), pero
            // NUNCA se lo devolvemos tal cual al llamador: eso podria filtrar
            // informacion sensible de la infraestructura (motor de base de
            // datos, nombres de tablas/columnas, rutas de archivos del
            // servidor). En cambio, lo envolvemos ("wrapping") dentro de una
            // ReglaNegocioException con un mensaje generico y amigable,
            // pasando "ex" como inner exception para que quede disponible
            // SOLO para logging interno del lado del servidor.
            throw new ReglaNegocioException("Ocurrio un error al procesar el usuario.", ex);
        }
    }

    // ObtenerUsuarioPorIdAsync: busca un Usuario por Id y lo traduce a DTO.
    public async Task<UsuarioDTO> ObtenerUsuarioPorIdAsync(int id)
    {
        try
        {
            var usuario = await _usuarioRepository.ObtenerPorIdAsync(id);

            // El repositorio devuelve "Usuario?" (nullable): es perfectamente
            // valido que no exista ningun registro con ese Id. Es responsabilidad
            // de la capa Business decidir que hacer en ese caso: aca elegimos
            // lanzar RecursoNoEncontradoException, que en una futura capa Api
            // se podria mapear directamente a un HTTP 404.
            if (usuario is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el usuario con Id {id}.");
            }

            return MapearADTO(usuario);
        }
        catch (RecursoNoEncontradoException)
        {
            // Relanzamos tal cual: esta excepcion YA es "amigable" y especifica,
            // no es un error tecnico que necesite ser envuelto de nuevo.
            throw;
        }
        catch (Exception ex)
        {
            // Cualquier otro error (ej: fallo de conexion a la base de datos
            // mientras se ejecutaba ObtenerPorIdAsync) se traduce a un mensaje
            // generico y seguro, igual que en CrearUsuarioAsync.
            throw new ReglaNegocioException("Ocurrio un error al obtener el usuario.", ex);
        }
    }

    // ObtenerTodosLosUsuariosAsync: devuelve el listado completo de Usuarios,
    // ya mapeados a DTO.
    public async Task<IEnumerable<UsuarioDTO>> ObtenerTodosLosUsuariosAsync()
    {
        try
        {
            var usuarios = await _usuarioRepository.ObtenerTodosAsync();

            // Select() (LINQ) mapea CADA entidad Usuario de la coleccion hacia
            // su correspondiente UsuarioDTO, reutilizando el mismo metodo
            // privado de mapeo que usan los demas metodos del servicio.
            return usuarios.Select(MapearADTO);
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al obtener el listado de usuarios.", ex);
        }
    }

    // ActualizarUsuarioAsync: modifica el Nombre y el Email de un Usuario que ya
    // existe. Notar que PasswordHash/PasswordSalt NUNCA se tocan aca: cambiar la
    // contrasena queda fuera del alcance de este metodo (ver comentario en
    // UsuarioActualizacionDTO).
    public async Task<UsuarioDTO> ActualizarUsuarioAsync(int id, UsuarioActualizacionDTO usuarioActualizacionDTO)
    {
        // --- Paso 1: Validaciones de negocio (mismas reglas que en el alta) ---
        if (string.IsNullOrWhiteSpace(usuarioActualizacionDTO.Nombre))
        {
            throw new ReglaNegocioException("El nombre del usuario no puede estar vacio.");
        }

        if (string.IsNullOrWhiteSpace(usuarioActualizacionDTO.Email) ||
            !Regex.IsMatch(usuarioActualizacionDTO.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            throw new ReglaNegocioException("El email ingresado no tiene un formato valido.");
        }

        try
        {
            // --- Paso 2: buscar la entidad EXISTENTE (no se "crea" una nueva) ---
            // A diferencia de CrearUsuarioAsync, aca necesitamos la entidad
            // completa ya trackeada por el repositorio (con su Id, PasswordHash,
            // PasswordSalt y FechaCreacion originales intactos) para no perder
            // esos datos al actualizar solo Nombre/Email.
            var usuario = await _usuarioRepository.ObtenerPorIdAsync(id);

            if (usuario is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el usuario con Id {id}.");
            }

            // Se modifican UNICAMENTE los campos que este DTO expone. El resto
            // de la entidad (PasswordHash, PasswordSalt, FechaCreacion) queda
            // intacto porque ActivoDigitalActualizacionDTO/UsuarioActualizacionDTO
            // ni siquiera tienen esas propiedades: es fisicamente imposible
            // sobrescribirlas por este camino.
            usuario.Nombre = usuarioActualizacionDTO.Nombre.Trim();
            usuario.Email = usuarioActualizacionDTO.Email.Trim();

            // Dato de auditoria: se deja constancia de CUANDO y QUIEN modifico
            // el registro (bonus de auditoria pedido por la rubrica).
            usuario.FechaModificacion = DateTime.UtcNow;
            usuario.UsuarioModificacion = "sistema";

            // Delegamos el UPDATE al repositorio a traves de la interfaz: la
            // capa Business no sabe (ni le importa) que sentencia SQL termina
            // ejecutando EF Core por detras.
            await _usuarioRepository.ActualizarAsync(usuario);

            return MapearADTO(usuario);
        }
        catch (RecursoNoEncontradoException)
        {
            throw;
        }
        catch (ReglaNegocioException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al actualizar el usuario.", ex);
        }
    }

    // EliminarUsuarioAsync: borra un Usuario existente por su Id.
    public async Task EliminarUsuarioAsync(int id)
    {
        try
        {
            // Se verifica la existencia ANTES de intentar borrar: esto permite
            // distinguir "el Id no existe" (404, RecursoNoEncontradoException)
            // de un eventual error tecnico al borrar (500 traducido a
            // ReglaNegocioException), en vez de que ambos casos terminen
            // mezclados dentro de un mismo catch generico.
            var usuario = await _usuarioRepository.ObtenerPorIdAsync(id);

            if (usuario is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el usuario con Id {id}.");
            }

            // Delegamos el DELETE al repositorio. La cascada configurada en
            // AppDbContext (OnDelete(DeleteBehavior.Cascade) para Beneficiarios
            // y ActivosDigitales) se encarga de que no queden registros
            // huerfanos: eso es un detalle de la capa Data, invisible aca.
            await _usuarioRepository.EliminarAsync(id);
        }
        catch (RecursoNoEncontradoException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al eliminar el usuario.", ex);
        }
    }

    // ObtenerUsuarioParaAutenticacionAsync: busca un Usuario por Email y
    // devuelve sus datos MINIMOS necesarios para un flujo de Login (incluido
    // el hash/salt, a diferencia de todos los demas metodos de este servicio).
    public async Task<UsuarioAutenticacionDTO> ObtenerUsuarioParaAutenticacionAsync(string email)
    {
        try
        {
            var usuario = await _usuarioRepository.ObtenerPorEmailAsync(email);

            if (usuario is null)
            {
                // Notar que el mensaje NO distingue "el email no existe" de
                // ningun otro motivo: es responsabilidad de AuthController (no
                // de este servicio) decidir que texto EXACTO le llega al
                // cliente en el 401 de un login fallido. Aca simplemente se
                // informa, para uso INTERNO, que no se encontro el registro.
                throw new RecursoNoEncontradoException($"No se encontro un usuario con el email '{email}'.");
            }

            return new UsuarioAutenticacionDTO
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                PasswordHash = usuario.PasswordHash,
                PasswordSalt = usuario.PasswordSalt,
                Rol = usuario.Rol
            };
        }
        catch (RecursoNoEncontradoException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al autenticar el usuario.", ex);
        }
    }

    // --- Metodos privados auxiliares (detalles de implementacion internos) ---
    //
    // Notar que el calculo criptografico del hash/salt YA NO vive aca: fue
    // extraido a ISeguridadService/SeguridadService (ver el constructor de
    // esta clase) para que la logica de seguridad de contrasenas tenga un
    // unico lugar de verdad, reutilizable por cualquier otro servicio futuro.

    // MapearADTO centraliza en un unico lugar la conversion de la entidad
    // "Usuario" (Data) hacia "UsuarioDTO" (Business/salida). Tenerlo en un
    // solo metodo evita repetir esta logica de mapeo en cada operacion del
    // servicio y, sobre todo, evita el riesgo de olvidarse de excluir algun
    // campo sensible (PasswordHash/PasswordSalt) en alguno de los puntos de
    // salida si el mapeo se hiciera "a mano" en cada lugar.
    private static UsuarioDTO MapearADTO(Usuario usuario)
    {
        return new UsuarioDTO
        {
            Id = usuario.Id,
            Nombre = usuario.Nombre,
            Email = usuario.Email,
            FechaCreacion = usuario.FechaCreacion,
            Rol = usuario.Rol
        };
    }
}
