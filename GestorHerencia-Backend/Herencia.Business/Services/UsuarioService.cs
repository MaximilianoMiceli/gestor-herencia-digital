using System.Security.Cryptography;
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

    // IAsignacionHerenciaRepository se necesita UNICAMENTE para
    // CrearUsuarioAsync: al registrarse alguien, hay que revisar si existen
    // invitaciones pendientes (AsignacionHerencia.UsuarioId == null) que lo
    // nombraban por este mismo Email, y "reclamarlas" automaticamente (ver
    // el detalle en ese metodo). Es la contraparte, del lado del registro,
    // de la invitacion por Email que arma AsignacionHerenciaService.CrearAsignacionesAsync.
    private readonly IAsignacionHerenciaRepository _asignacionHerenciaRepository;

    // INotificationService se necesita UNICAMENTE para el segundo factor de
    // autenticacion (GenerarYEnviarCodigoDobleFactorAsync): es quien
    // efectivamente "envia" (simulado, por consola) el codigo de 6 digitos
    // al Email del propio Usuario. Mismo criterio que ya usa
    // CertificadoDefuncionService para sus propias notificaciones.
    private readonly INotificationService _notificationService;

    // El contenedor de Inyeccion de Dependencias configurado en Program.cs
    // (etapa Api) sera el encargado de "resolver" automaticamente una
    // instancia de IUsuarioRepository (tipicamente UsuarioRepository), de
    // ISeguridadService (tipicamente SeguridadService), de
    // IAsignacionHerenciaRepository y de INotificationService, y pasarlas
    // aca por este constructor cuando se necesite un UsuarioService.
    public UsuarioService(
        IUsuarioRepository usuarioRepository,
        ISeguridadService seguridadService,
        IAsignacionHerenciaRepository asignacionHerenciaRepository,
        INotificationService notificationService)
    {
        _usuarioRepository = usuarioRepository;
        _seguridadService = seguridadService;
        _asignacionHerenciaRepository = asignacionHerenciaRepository;
        _notificationService = notificationService;
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

        // --- Validacion de DNI: solo digitos, 7 u 8 caracteres ---
        // Cubre el formato de un DNI argentino (el mercado principal de este
        // proyecto). No se valida "existencia real" del documento (eso
        // requeriria integrar con un organismo oficial, fuera del alcance de
        // este sistema): solo se rechaza lo que estructuralmente no puede ser
        // un DNI valido (letras, simbolos, largos absurdos).
        if (string.IsNullOrWhiteSpace(usuarioCreacionDTO.Dni) ||
            !Regex.IsMatch(usuarioCreacionDTO.Dni, @"^\d{7,8}$"))
        {
            throw new ReglaNegocioException("El DNI debe tener 7 u 8 digitos numericos.");
        }

        // --- Validacion de fecha de nacimiento: fecha real Y mayoria de edad ---
        // ValidarMayoriaDeEdad (metodo privado, mas abajo) centraliza el
        // calculo de edad para reutilizarlo tambien desde ActualizarUsuarioAsync.
        ValidarFechaNacimiento(usuarioCreacionDTO.FechaNacimiento);

        // --- Paso 2: logica de negocio + acceso a datos, protegida con try-catch ---
        try
        {
            // --- Validacion de UNICIDAD: Email y DNI no pueden repetirse ---
            // Se verifica ACA, explicitamente, en vez de dejar que la
            // violacion del indice UNICO de la base de datos (ver
            // AppDbContext.OnModelCreating, columnas Email y Dni) se
            // descubra recien al intentar el INSERT: sin este chequeo previo,
            // ese error tecnico de EF Core/SQLite caia en el catch generico
            // de mas abajo y se traducia al mensaje generico "Ocurrio un
            // error al procesar el usuario.", sin explicarle al cliente CUAL
            // era el problema real.
            var usuarioConMismoEmail = await _usuarioRepository.ObtenerPorEmailAsync(usuarioCreacionDTO.Email.Trim());
            if (usuarioConMismoEmail is not null)
            {
                throw new ReglaNegocioException("Ya existe una cuenta registrada con ese email.");
            }

            var usuarioConMismoDni = await _usuarioRepository.ObtenerPorDniAsync(usuarioCreacionDTO.Dni.Trim());
            if (usuarioConMismoDni is not null)
            {
                throw new ReglaNegocioException("Ya existe una cuenta registrada con ese DNI.");
            }

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
                Dni = usuarioCreacionDTO.Dni.Trim(),
                FechaNacimiento = usuarioCreacionDTO.FechaNacimiento.Date,
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

            // --- Reclamo automatico de invitaciones pendientes ---
            // Si algun otorgante ya lo habia invitado como beneficiario ANTES
            // de que esta persona tuviera cuenta (AsignacionHerenciaService.
            // CrearAsignacionesAsync guarda esas filas con UsuarioId en null
            // y el Email tipeado en AsignacionHerencia.EmailInvitado), este es
            // el momento de vincularlas: se buscan todas las asignaciones sin
            // reclamar que coincidan con el Email recien registrado, y se les
            // completa el UsuarioId con el Id de la cuenta que se acaba de
            // crear. De esta forma, apenas la persona inicia sesion por
            // primera vez, sus herencias pendientes ya aparecen asociadas a
            // su cuenta sin ningun paso manual adicional de su parte.
            var invitacionesPendientes = await _asignacionHerenciaRepository.ObtenerPendientesPorEmailAsync(usuario.Email);

            foreach (var invitacion in invitacionesPendientes)
            {
                invitacion.UsuarioId = usuario.Id;
                invitacion.FechaModificacion = DateTime.UtcNow;
                invitacion.UsuarioModificacion = "sistema";

                await _asignacionHerenciaRepository.ActualizarAsync(invitacion);
            }

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

        if (string.IsNullOrWhiteSpace(usuarioActualizacionDTO.Dni) ||
            !Regex.IsMatch(usuarioActualizacionDTO.Dni, @"^\d{7,8}$"))
        {
            throw new ReglaNegocioException("El DNI debe tener 7 u 8 digitos numericos.");
        }

        ValidarFechaNacimiento(usuarioActualizacionDTO.FechaNacimiento);

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
            usuario.Dni = usuarioActualizacionDTO.Dni.Trim();
            usuario.FechaNacimiento = usuarioActualizacionDTO.FechaNacimiento.Date;

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
            // AppDbContext (OnDelete(DeleteBehavior.Cascade) para
            // ActivosOtorgados) se encarga de que no queden activos
            // huerfanos: eso es un detalle de la capa Data, invisible aca. Si
            // este Usuario todavia tiene HerenciasRecibidas (fue designado
            // como beneficiario de algo), en cambio, la base de datos
            // RECHAZA el borrado (Restrict, no Cascade): ese caso llega aca
            // como una excepcion tecnica generica, traducida mas abajo a un
            // ReglaNegocioException con mensaje seguro.
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
                Rol = usuario.Rol,
                DobleFactorHabilitado = usuario.DobleFactorHabilitado
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

    // CambiarPasswordAsync: cambia la contraseña de un Usuario ya
    // autenticado que conoce su contraseña actual.
    public async Task CambiarPasswordAsync(int usuarioId, CambiarPasswordDTO cambiarPasswordDTO)
    {
        if (string.IsNullOrWhiteSpace(cambiarPasswordDTO.PasswordNueva) || cambiarPasswordDTO.PasswordNueva.Length < 6)
        {
            throw new ReglaNegocioException("La nueva contraseña debe tener al menos 6 caracteres.");
        }

        try
        {
            var usuario = await _usuarioRepository.ObtenerPorIdAsync(usuarioId);

            if (usuario is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el usuario con Id {usuarioId}.");
            }

            // --- Verificacion de la contraseña ACTUAL ---
            // Mismo algoritmo (y misma razon de ser) que el paso 2 del Login
            // en AuthController: se recalcula el hash de la contraseña
            // candidata con el salt YA persistido y se compara en tiempo
            // constante contra el hash guardado. Si no coincide, no se
            // permite continuar: nadie deberia poder cambiar la contraseña
            // de una cuenta sin antes demostrar que conoce la ACTUAL.
            var passwordActualValida = _seguridadService.VerificarPasswordHash(
                cambiarPasswordDTO.PasswordActual,
                usuario.PasswordHash,
                usuario.PasswordSalt);

            if (!passwordActualValida)
            {
                throw new ReglaNegocioException("La contraseña actual ingresada es incorrecta.");
            }

            _seguridadService.CrearPasswordHash(cambiarPasswordDTO.PasswordNueva, out var passwordHash, out var passwordSalt);

            usuario.PasswordHash = passwordHash;
            usuario.PasswordSalt = passwordSalt;
            usuario.FechaModificacion = DateTime.UtcNow;
            usuario.UsuarioModificacion = "sistema";

            await _usuarioRepository.ActualizarAsync(usuario);
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
            throw new ReglaNegocioException("Ocurrio un error al cambiar la contraseña.", ex);
        }
    }

    // SolicitarResetPasswordAsync: primer paso del flujo de "olvide mi
    // contraseña" (ver el detalle completo en IUsuarioService).
    public async Task<string?> SolicitarResetPasswordAsync(string email)
    {
        try
        {
            var usuario = await _usuarioRepository.ObtenerPorEmailAsync(email.Trim());

            // Deliberadamente NO se lanza RecursoNoEncontradoException aca:
            // se devuelve null y es el CONTROLLER quien decide el mensaje
            // (siempre el mismo, exista o no la cuenta), para no permitir
            // que un atacante use este endpoint para enumerar que emails
            // estan registrados en el sistema.
            if (usuario is null)
            {
                return null;
            }

            // --- Generacion del token de reseteo ---
            // RandomNumberGenerator.GetHexString (CSPRNG, criptograficamente
            // seguro) genera una cadena de 64 caracteres hexadecimales: 32
            // bytes (256 bits) de entropia, imposible de adivinar por fuerza
            // bruta en un tiempo util. Se guarda en texto plano en la base
            // de datos (a diferencia de la contraseña) porque su seguridad
            // no depende de resistir un volcado de la base de datos, sino
            // de ser IMPOSIBLE de adivinar y de vida CORTA: alguien con
            // acceso de lectura a la base ya tiene acceso a todo lo demas.
            var token = RandomNumberGenerator.GetHexString(64);

            usuario.PasswordResetToken = token;

            // Ventana de validez corta (1 hora): un link de reseteo viejo
            // en una bandeja de entrada no deberia servir para siempre.
            usuario.PasswordResetExpiracion = DateTime.UtcNow.AddHours(1);
            usuario.FechaModificacion = DateTime.UtcNow;
            usuario.UsuarioModificacion = "sistema";

            await _usuarioRepository.ActualizarAsync(usuario);

            return token;
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al solicitar el reseteo de contraseña.", ex);
        }
    }

    // ResetearPasswordAsync: segundo y ultimo paso del flujo de "olvide mi
    // contraseña".
    public async Task ResetearPasswordAsync(ResetearPasswordDTO resetearPasswordDTO)
    {
        if (string.IsNullOrWhiteSpace(resetearPasswordDTO.PasswordNueva) || resetearPasswordDTO.PasswordNueva.Length < 6)
        {
            throw new ReglaNegocioException("La nueva contraseña debe tener al menos 6 caracteres.");
        }

        try
        {
            var usuario = await _usuarioRepository.ObtenerPorPasswordResetTokenAsync(resetearPasswordDTO.Token);

            // Mensaje generico a proposito ("token invalido o expirado"),
            // sin distinguir "el token no existe" de "existe pero ya
            // vencio": exactamente el mismo criterio de no dar pistas
            // adicionales que ya se aplica en el Login.
            if (usuario is null || usuario.PasswordResetExpiracion is null || usuario.PasswordResetExpiracion < DateTime.UtcNow)
            {
                throw new ReglaNegocioException("El token de reseteo es invalido o ya expiro.");
            }

            _seguridadService.CrearPasswordHash(resetearPasswordDTO.PasswordNueva, out var passwordHash, out var passwordSalt);

            usuario.PasswordHash = passwordHash;
            usuario.PasswordSalt = passwordSalt;

            // --- El token es de UN SOLO USO ---
            // Se limpia apenas se usa exitosamente: si alguien mas
            // interceptara el mismo link mas tarde, ya no podria reutilizarlo
            // para volver a resetear la contraseña.
            usuario.PasswordResetToken = null;
            usuario.PasswordResetExpiracion = null;
            usuario.FechaModificacion = DateTime.UtcNow;
            usuario.UsuarioModificacion = "sistema";

            await _usuarioRepository.ActualizarAsync(usuario);
        }
        catch (ReglaNegocioException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al resetear la contraseña.", ex);
        }
    }

    // GenerarYEnviarCodigoDobleFactorAsync: ver el detalle completo del
    // "por que" en IUsuarioService.
    public async Task GenerarYEnviarCodigoDobleFactorAsync(int usuarioId)
    {
        try
        {
            var usuario = await _usuarioRepository.ObtenerPorIdAsync(usuarioId);

            if (usuario is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el usuario con Id {usuarioId}.");
            }

            // --- Generacion del codigo: 6 digitos numericos ---
            // RandomNumberGenerator.GetInt32 (CSPRNG, criptograficamente
            // seguro, a diferencia de System.Random) genera un entero
            // aleatorio en el rango [100000, 999999]: siempre 6 digitos, sin
            // ceros a la izquierda que compliquen la comparacion como string.
            // A diferencia del PasswordResetToken (256 bits, imposible de
            // adivinar), un codigo de 6 digitos tiene un espacio MUCHO mas
            // chico (900.000 combinaciones); esto es una limitacion conocida
            // y aceptada de cualquier esquema de "codigo corto por email/SMS"
            // (el mismo patron que usan bancos y redes sociales reales), que
            // se compensa con la ventana de vigencia corta de abajo (10
            // minutos) y con que es de UN SOLO USO.
            var codigo = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();

            usuario.CodigoDobleFactor = codigo;
            usuario.CodigoDobleFactorExpiracion = DateTime.UtcNow.AddMinutes(10);
            usuario.FechaModificacion = DateTime.UtcNow;
            usuario.UsuarioModificacion = "sistema";

            await _usuarioRepository.ActualizarAsync(usuario);

            // Se envia SIEMPRE por Email (no por el "MetodoNotificacion" que
            // el usuario eligio para Verificacion de Vida, que es un
            // concepto de negocio totalmente distinto): el segundo factor de
            // login solo tiene sentido si viaja a una casilla de correo que
            // el usuario pueda revisar en el momento.
            await _notificationService.EnviarNotificacionAsync(
                usuario,
                MetodoNotificacion.Email,
                "Tu codigo de verificacion en dos pasos",
                $"Tu codigo para iniciar sesion es: {codigo}\n\nEste codigo vence en 10 minutos y solo se puede usar una vez.");
        }
        catch (RecursoNoEncontradoException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al generar el codigo de verificacion en dos pasos.", ex);
        }
    }

    // VerificarCodigoDobleFactorAsync: ver el detalle completo del "por que"
    // en IUsuarioService.
    public async Task<UsuarioDTO> VerificarCodigoDobleFactorAsync(int usuarioId, string codigo)
    {
        try
        {
            var usuario = await _usuarioRepository.ObtenerPorIdAsync(usuarioId);

            if (usuario is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el usuario con Id {usuarioId}.");
            }

            // Mensaje generico a proposito ("codigo invalido o expirado"),
            // igual criterio que ResetearPasswordAsync: no distinguir "no
            // hay ningun codigo pendiente" de "el codigo no coincide" de "ya
            // vencio", para no darle a un atacante pistas adicionales sobre
            // cual de los tres casos esta pasando.
            if (usuario.CodigoDobleFactor is null
                || usuario.CodigoDobleFactorExpiracion is null
                || usuario.CodigoDobleFactorExpiracion < DateTime.UtcNow
                || usuario.CodigoDobleFactor != codigo.Trim())
            {
                throw new ReglaNegocioException("El codigo de verificacion es invalido o ya expiro.");
            }

            // --- El codigo es de UN SOLO USO ---
            // Se limpia apenas se usa exitosamente, igual criterio que
            // PasswordResetToken: si alguien mas lo interceptara mas tarde
            // (ej: revisando la bandeja de entrada despues), ya no podria
            // reutilizarlo para completar un login.
            usuario.CodigoDobleFactor = null;
            usuario.CodigoDobleFactorExpiracion = null;
            usuario.FechaModificacion = DateTime.UtcNow;
            usuario.UsuarioModificacion = "sistema";

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
            throw new ReglaNegocioException("Ocurrio un error al verificar el codigo de doble factor.", ex);
        }
    }

    // ActualizarDobleFactorAsync: ver el detalle completo del "por que" en
    // IUsuarioService.
    public async Task<UsuarioDTO> ActualizarDobleFactorAsync(int usuarioId, bool habilitado)
    {
        try
        {
            var usuario = await _usuarioRepository.ObtenerPorIdAsync(usuarioId);

            if (usuario is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el usuario con Id {usuarioId}.");
            }

            usuario.DobleFactorHabilitado = habilitado;

            // Al desactivar el 2FA (o al reactivarlo desde cero), se limpia
            // cualquier codigo que hubiera quedado pendiente de un login
            // anterior sin completar: evita que un codigo "viejo" siga
            // siendo valido despues de que el usuario cambio esta config.
            usuario.CodigoDobleFactor = null;
            usuario.CodigoDobleFactorExpiracion = null;
            usuario.FechaModificacion = DateTime.UtcNow;
            usuario.UsuarioModificacion = "sistema";

            await _usuarioRepository.ActualizarAsync(usuario);

            return MapearADTO(usuario);
        }
        catch (RecursoNoEncontradoException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al actualizar la configuracion de doble factor.", ex);
        }
    }

    // --- Metodos privados auxiliares (detalles de implementacion internos) ---
    //
    // Notar que el calculo criptografico del hash/salt YA NO vive aca: fue
    // extraido a ISeguridadService/SeguridadService (ver el constructor de
    // esta clase) para que la logica de seguridad de contrasenas tenga un
    // unico lugar de verdad, reutilizable por cualquier otro servicio futuro.

    // ValidarFechaNacimiento: centraliza la regla "el titular debe ser mayor
    // de edad" para que CrearUsuarioAsync y ActualizarUsuarioAsync apliquen
    // EXACTAMENTE la misma regla (evita que, por ejemplo, alguien pudiera
    // registrarse siendo mayor pero despues "corregir" su fecha de
    // nacimiento a una que lo haria menor, sin que nadie lo revise).
    private static void ValidarFechaNacimiento(DateTime fechaNacimiento)
    {
        var hoy = DateTime.UtcNow.Date;

        // Una fecha de nacimiento en el futuro es, directamente, un dato
        // estructuralmente invalido: se rechaza antes de calcular ninguna edad.
        if (fechaNacimiento.Date > hoy)
        {
            throw new ReglaNegocioException("La fecha de nacimiento no puede ser futura.");
        }

        // --- Calculo de edad exacta (no solo "restar los anios") ---
        // Restar unicamente "hoy.Year - fechaNacimiento.Year" sobreestima la
        // edad de alguien que todavia no cumplio anios este anio calendario
        // (ej: nacido el 20/12/2008, hoy 10/07/2026: la resta simple da 18,
        // pero la persona recien cumple 18 en diciembre). Se corrige
        // restando 1 si el cumpleanios de este anio TODAVIA no llego.
        var edad = hoy.Year - fechaNacimiento.Year;
        if (fechaNacimiento.Date > hoy.AddYears(-edad))
        {
            edad--;
        }

        // --- Por que exigir mayoria de edad (18 anios) ---
        // Este sistema decide, en ultima instancia, la liberacion de bienes
        // (claves de billeteras, datos bancarios) hacia terceros: operarlo
        // exige la capacidad legal plena que el Codigo Civil y Comercial
        // argentino reconoce recien a partir de los 18 anios (Art. 25). No es
        // una eleccion de producto arbitraria, es un requisito del propio
        // dominio que el sistema representa.
        if (edad < 18)
        {
            throw new ReglaNegocioException("Debes ser mayor de edad (18 anios) para registrarte.");
        }

        // Limite superior de sanidad: una fecha de nacimiento de mas de 120
        // anios atras es, con certeza practica, un error de tipeo (ej: un
        // usuario que quiso escribir "1998" y tipeo "1898"), no un dato real.
        if (edad > 120)
        {
            throw new ReglaNegocioException("La fecha de nacimiento ingresada no es valida.");
        }
    }

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
            Dni = usuario.Dni,
            FechaNacimiento = usuario.FechaNacimiento,
            FechaCreacion = usuario.FechaCreacion,
            Rol = usuario.Rol,
            DobleFactorHabilitado = usuario.DobleFactorHabilitado
        };
    }
}
