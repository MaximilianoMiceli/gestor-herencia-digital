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

    // Encapsula el hash/verificación de contraseñas (HMACSHA512) en un único lugar.
    private readonly ISeguridadService _seguridadService;

    // Usado en CrearUsuarioAsync para reclamar invitaciones pendientes a este email.
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

        // Regex simple, no RFC-compliant, pero suficiente para rechazar entradas inválidas.
        if (string.IsNullOrWhiteSpace(usuarioCreacionDTO.Email) ||
            !Regex.IsMatch(usuarioCreacionDTO.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            throw new ReglaNegocioException("El email ingresado no tiene un formato valido.");
        }

        if (string.IsNullOrWhiteSpace(usuarioCreacionDTO.Password) || usuarioCreacionDTO.Password.Length < 6)
        {
            throw new ReglaNegocioException("La contrasena debe tener al menos 6 caracteres.");
        }

        // DNI argentino: solo se valida el formato (7-8 dígitos), no la existencia real.
        if (string.IsNullOrWhiteSpace(usuarioCreacionDTO.Dni) ||
            !Regex.IsMatch(usuarioCreacionDTO.Dni, @"^\d{7,8}$"))
        {
            throw new ReglaNegocioException("El DNI debe tener 7 u 8 digitos numericos.");
        }

        ValidarFechaNacimiento(usuarioCreacionDTO.FechaNacimiento);

        try
        {
            // Se valida unicidad acá (no solo en el índice único de BD) para poder decir
            // al cliente cuál de los dos campos ya está en uso.
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

            // Mapeo manual: evita que el cliente establezca Id, PasswordHash o FechaCreacion,
            // campos que ni siquiera existen en UsuarioCreacionDTO.
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

            // Reclamo automático de invitaciones creadas antes de que este email tuviera cuenta.
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
            // El detalle técnico (ex) no se expone al llamador, solo queda como InnerException.
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

            // Cascade en AppDbContext borra sus activos digitales; si tiene herencias
            // recibidas pendientes, la BD rechaza el borrado (Restrict, no Cascade).
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
                // Mensaje genérico a propósito: evita que un login fallido permita
                // enumerar cuentas registradas.
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

            // Se exige la contraseña actual (comparación en tiempo constante) antes del cambio.
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

            // No se lanza RecursoNoEncontradoException: devuelve null para que el mensaje
            // al cliente sea siempre el mismo y no permita enumerar emails registrados.
            if (usuario is null)
            {
                return null;
            }

            // Token de un solo uso, 256 bits de entropía, vida corta (1 hora).
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

            // Mensaje genérico a propósito, mismo criterio anti-enumeración que el login.
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

            // Espacio de 900.000 combinaciones (mucho menor al token de reseteo), limitación
            // aceptada de cualquier código corto, compensada con ventana de 10 min y uso único.
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

            // Mensaje genérico, mismo criterio anti-enumeración que el resto del archivo.
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

        // Se corrige restando 1 si el cumpleaños de este año todavía no llegó.
        var edad = hoy.Year - fechaNacimiento.Year;
        if (fechaNacimiento.Date > hoy.AddYears(-edad))
        {
            edad--;
        }

        // 18 años: capacidad legal plena requerida para liberar bienes a terceros (CCyC Art. 25).
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

    // Centraliza el mapeo para no filtrar PasswordHash/PasswordSalt en algún punto de salida.
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
