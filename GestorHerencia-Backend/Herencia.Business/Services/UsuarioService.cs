using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Herencia.Business.Dtos;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Herencia.Data.Models;
using Herencia.Data.Repositories;

namespace Herencia.Business.Services;

/// <summary>
/// Implementación de <see cref="IUsuarioService"/>: validaciones de negocio, orquestación
/// del hash de contraseña, autenticación en dos pasos, y mapeo entre entidades y DTOs.
/// </summary>
public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _usuarioRepository;

    // Encapsula el algoritmo criptográfico (HMACSHA512) de hash/verificación de
    // contraseñas, para que esa lógica de seguridad viva en un único lugar (ver SeguridadService).
    private readonly ISeguridadService _seguridadService;

    // Se usa únicamente en CrearUsuarioAsync, para reclamar invitaciones pendientes
    // (AsignacionHerencia.UsuarioId == null) que ya nombraban a este email como beneficiario.
    private readonly IAsignacionHerenciaRepository _asignacionHerenciaRepository;

    // Se usa únicamente para enviar el código de doble factor de autenticación.
    private readonly INotificationService _notificationService;

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

    /// <summary>
    /// Da de alta un nuevo usuario: valida los datos de entrada, calcula el hash de la
    /// contraseña y reclama automáticamente cualquier invitación pendiente a su email.
    /// </summary>
    public async Task<UsuarioDTO> CrearUsuarioAsync(UsuarioCreacionDTO usuarioCreacionDTO)
    {
        if (string.IsNullOrWhiteSpace(usuarioCreacionDTO.Nombre))
        {
            throw new ReglaNegocioException("El nombre del usuario no puede estar vacio.");
        }

        // Regex simple, no 100% RFC-compliant, pero suficiente para rechazar entradas
        // claramente inválidas antes de persistirlas.
        if (string.IsNullOrWhiteSpace(usuarioCreacionDTO.Email) ||
            !Regex.IsMatch(usuarioCreacionDTO.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            throw new ReglaNegocioException("El email ingresado no tiene un formato valido.");
        }

        if (string.IsNullOrWhiteSpace(usuarioCreacionDTO.Password) || usuarioCreacionDTO.Password.Length < 6)
        {
            throw new ReglaNegocioException("La contrasena debe tener al menos 6 caracteres.");
        }

        // DNI argentino: solo se valida el formato estructural (7 u 8 dígitos), no la
        // existencia real del documento (requeriría integrar con un organismo oficial).
        if (string.IsNullOrWhiteSpace(usuarioCreacionDTO.Dni) ||
            !Regex.IsMatch(usuarioCreacionDTO.Dni, @"^\d{7,8}$"))
        {
            throw new ReglaNegocioException("El DNI debe tener 7 u 8 digitos numericos.");
        }

        ValidarFechaNacimiento(usuarioCreacionDTO.FechaNacimiento);

        try
        {
            // Se valida unicidad explícitamente en vez de esperar a que el índice único de
            // la base de datos (ver AppDbContext.OnModelCreating) rechace el INSERT: así se
            // puede informar al cliente cuál de los dos campos ya está en uso.
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

            _seguridadService.CrearPasswordHash(usuarioCreacionDTO.Password, out var passwordHash, out var passwordSalt);

            // El mapeo manual DTO -> Entidad impide que el cliente establezca un Id, un
            // PasswordHash falso o una FechaCreacion arbitraria: esos campos ni siquiera
            // existen en UsuarioCreacionDTO.
            var usuario = new Usuario
            {
                Nombre = usuarioCreacionDTO.Nombre.Trim(),
                Email = usuarioCreacionDTO.Email.Trim(),
                Dni = usuarioCreacionDTO.Dni.Trim(),
                FechaNacimiento = usuarioCreacionDTO.FechaNacimiento.Date,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                FechaCreacion = DateTime.UtcNow,
                UsuarioCreacion = "sistema"
            };

            await _usuarioRepository.AgregarAsync(usuario);

            // Reclamo automático: las invitaciones creadas antes de que este email tuviera
            // cuenta (UsuarioId null, ver AsignacionHerenciaService.CrearAsignacionesAsync)
            // se vinculan ahora al usuario recién creado.
            var invitacionesPendientes = await _asignacionHerenciaRepository.ObtenerPendientesPorEmailAsync(usuario.Email);

            foreach (var invitacion in invitacionesPendientes)
            {
                invitacion.UsuarioId = usuario.Id;
                invitacion.FechaModificacion = DateTime.UtcNow;
                invitacion.UsuarioModificacion = "sistema";

                await _asignacionHerenciaRepository.ActualizarAsync(invitacion);
            }

            return MapearADTO(usuario);
        }
        catch (ReglaNegocioException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // El detalle técnico real (ex) nunca se expone al llamador; se conserva solo
            // como InnerException para diagnóstico interno. Mismo criterio en todo el archivo.
            throw new ReglaNegocioException("Ocurrio un error al procesar el usuario.", ex);
        }
    }

    /// <summary>
    /// Busca un usuario por Id.
    /// </summary>
    public async Task<UsuarioDTO> ObtenerUsuarioPorIdAsync(int id)
    {
        try
        {
            var usuario = await _usuarioRepository.ObtenerPorIdAsync(id);

            if (usuario is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el usuario con Id {id}.");
            }

            return MapearADTO(usuario);
        }
        catch (RecursoNoEncontradoException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al obtener el usuario.", ex);
        }
    }

    /// <summary>
    /// Devuelve el listado completo de usuarios.
    /// </summary>
    public async Task<IEnumerable<UsuarioDTO>> ObtenerTodosLosUsuariosAsync()
    {
        try
        {
            var usuarios = await _usuarioRepository.ObtenerTodosAsync();

            return usuarios.Select(MapearADTO);
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al obtener el listado de usuarios.", ex);
        }
    }

    /// <summary>
    /// Actualiza nombre, email, DNI y fecha de nacimiento de un usuario existente.
    /// La contraseña nunca se modifica por este medio.
    /// </summary>
    public async Task<UsuarioDTO> ActualizarUsuarioAsync(int id, UsuarioActualizacionDTO usuarioActualizacionDTO)
    {
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
            var usuario = await _usuarioRepository.ObtenerPorIdAsync(id);

            if (usuario is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el usuario con Id {id}.");
            }

            usuario.Nombre = usuarioActualizacionDTO.Nombre.Trim();
            usuario.Email = usuarioActualizacionDTO.Email.Trim();
            usuario.Dni = usuarioActualizacionDTO.Dni.Trim();
            usuario.FechaNacimiento = usuarioActualizacionDTO.FechaNacimiento.Date;
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
            throw new ReglaNegocioException("Ocurrio un error al actualizar el usuario.", ex);
        }
    }

    /// <summary>
    /// Elimina un usuario existente por su Id.
    /// </summary>
    public async Task EliminarUsuarioAsync(int id)
    {
        try
        {
            var usuario = await _usuarioRepository.ObtenerPorIdAsync(id);

            if (usuario is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el usuario con Id {id}.");
            }

            // La cascada configurada en AppDbContext elimina también sus activos digitales
            // otorgados. Si el usuario todavía tiene herencias recibidas pendientes, en
            // cambio, la base de datos rechaza el borrado (Restrict, no Cascade).
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

    /// <summary>
    /// Busca un usuario por email y devuelve los datos mínimos necesarios para el login,
    /// incluido el hash/salt de la contraseña.
    /// </summary>
    public async Task<UsuarioAutenticacionDTO> ObtenerUsuarioParaAutenticacionAsync(string email)
    {
        try
        {
            var usuario = await _usuarioRepository.ObtenerPorEmailAsync(email);

            if (usuario is null)
            {
                // El mensaje no distingue "el email no existe" de ningún otro motivo: es
                // responsabilidad del controller decidir el texto exacto que ve el cliente
                // en un login fallido, para no permitir enumerar cuentas registradas.
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

    /// <summary>
    /// Cambia la contraseña de un usuario ya autenticado que conoce su contraseña actual.
    /// </summary>
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

            // Se exige demostrar conocimiento de la contraseña actual (comparación en
            // tiempo constante vía ISeguridadService) antes de permitir el cambio.
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

    /// <summary>
    /// Primer paso del flujo de "olvidé mi contraseña": genera un token de reseteo si el
    /// email corresponde a una cuenta existente.
    /// </summary>
    public async Task<string?> SolicitarResetPasswordAsync(string email)
    {
        try
        {
            var usuario = await _usuarioRepository.ObtenerPorEmailAsync(email.Trim());

            // Deliberadamente no se lanza RecursoNoEncontradoException: se devuelve null y
            // es el controller quien decide el mensaje (siempre el mismo, exista o no la
            // cuenta), para no permitir enumerar emails registrados.
            if (usuario is null)
            {
                return null;
            }

            // Token de un solo uso, criptográficamente aleatorio (256 bits de entropía) y
            // de vida corta (1 hora): un link de reseteo viejo no debería servir para siempre.
            var token = RandomNumberGenerator.GetHexString(64);

            usuario.PasswordResetToken = token;
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

    /// <summary>
    /// Segundo paso del flujo de "olvidé mi contraseña": valida el token y establece la
    /// nueva contraseña.
    /// </summary>
    public async Task ResetearPasswordAsync(ResetearPasswordDTO resetearPasswordDTO)
    {
        if (string.IsNullOrWhiteSpace(resetearPasswordDTO.PasswordNueva) || resetearPasswordDTO.PasswordNueva.Length < 6)
        {
            throw new ReglaNegocioException("La nueva contraseña debe tener al menos 6 caracteres.");
        }

        try
        {
            var usuario = await _usuarioRepository.ObtenerPorPasswordResetTokenAsync(resetearPasswordDTO.Token);

            // Mensaje genérico a propósito: no distingue "el token no existe" de "existe
            // pero ya venció", mismo criterio anti-enumeración que el login.
            if (usuario is null || usuario.PasswordResetExpiracion is null || usuario.PasswordResetExpiracion < DateTime.UtcNow)
            {
                throw new ReglaNegocioException("El token de reseteo es invalido o ya expiro.");
            }

            _seguridadService.CrearPasswordHash(resetearPasswordDTO.PasswordNueva, out var passwordHash, out var passwordSalt);

            usuario.PasswordHash = passwordHash;
            usuario.PasswordSalt = passwordSalt;

            // El token se limpia apenas se usa exitosamente: es de un solo uso.
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

    /// <summary>
    /// Genera un código de doble factor de 6 dígitos y lo envía por email al usuario.
    /// </summary>
    public async Task GenerarYEnviarCodigoDobleFactorAsync(int usuarioId)
    {
        try
        {
            var usuario = await _usuarioRepository.ObtenerPorIdAsync(usuarioId);

            if (usuario is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el usuario con Id {usuarioId}.");
            }

            // RandomNumberGenerator.GetInt32 (CSPRNG) genera un entero en [100000, 999999].
            // El espacio de 900.000 combinaciones es mucho más chico que el del token de
            // reseteo (256 bits); es una limitación conocida y aceptada de cualquier código
            // corto por email/SMS, compensada por la ventana corta (10 min) y el uso único.
            var codigo = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();

            usuario.CodigoDobleFactor = codigo;
            usuario.CodigoDobleFactorExpiracion = DateTime.UtcNow.AddMinutes(10);
            usuario.FechaModificacion = DateTime.UtcNow;
            usuario.UsuarioModificacion = "sistema";

            await _usuarioRepository.ActualizarAsync(usuario);

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

    /// <summary>
    /// Valida el código de doble factor de un usuario y lo consume (uso único).
    /// </summary>
    public async Task<UsuarioDTO> VerificarCodigoDobleFactorAsync(int usuarioId, string codigo)
    {
        try
        {
            var usuario = await _usuarioRepository.ObtenerPorIdAsync(usuarioId);

            if (usuario is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el usuario con Id {usuarioId}.");
            }

            // Mensaje genérico, mismo criterio anti-enumeración: no distingue "no hay
            // código pendiente" de "no coincide" de "ya venció".
            if (usuario.CodigoDobleFactor is null
                || usuario.CodigoDobleFactorExpiracion is null
                || usuario.CodigoDobleFactorExpiracion < DateTime.UtcNow
                || usuario.CodigoDobleFactor != codigo.Trim())
            {
                throw new ReglaNegocioException("El codigo de verificacion es invalido o ya expiro.");
            }

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

    /// <summary>
    /// Activa o desactiva el doble factor de autenticación de un usuario, limpiando
    /// cualquier código pendiente de un login anterior sin completar.
    /// </summary>
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

    /// <summary>
    /// Valida que la fecha de nacimiento sea real y que el titular sea mayor de edad.
    /// Centralizada para que alta y actualización apliquen exactamente la misma regla.
    /// </summary>
    private static void ValidarFechaNacimiento(DateTime fechaNacimiento)
    {
        var hoy = DateTime.UtcNow.Date;

        if (fechaNacimiento.Date > hoy)
        {
            throw new ReglaNegocioException("La fecha de nacimiento no puede ser futura.");
        }

        // Restar solo los años calendario sobreestima la edad de quien todavía no cumplió
        // años este año; se corrige restando 1 si el cumpleaños de este año no llegó aún.
        var edad = hoy.Year - fechaNacimiento.Year;
        if (fechaNacimiento.Date > hoy.AddYears(-edad))
        {
            edad--;
        }

        // La mayoría de edad (18 años) no es una elección de producto: el sistema decide
        // la liberación de bienes hacia terceros, lo que exige la capacidad legal plena que
        // el Codigo Civil y Comercial argentino reconoce recien a partir de los 18 anios (Art. 25).
        if (edad < 18)
        {
            throw new ReglaNegocioException("Debes ser mayor de edad (18 anios) para registrarte.");
        }

        // Límite superior de sanidad: más de 120 años es, con certeza práctica, un error de tipeo.
        if (edad > 120)
        {
            throw new ReglaNegocioException("La fecha de nacimiento ingresada no es valida.");
        }
    }

    // Centraliza la conversión Usuario -> UsuarioDTO; evita repetir el mapeo y el riesgo de
    // filtrar accidentalmente PasswordHash/PasswordSalt en algún punto de salida.
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
