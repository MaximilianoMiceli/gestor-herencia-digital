using Herencia.Business.Dtos;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Herencia.Data.Models;
using Herencia.Data.Repositories;
using Microsoft.Extensions.Configuration;

namespace Herencia.Business.Services;

/// <summary>
/// Implementación de <see cref="IVerificacionVidaService"/>: configuración del monitoreo
/// de actividad, check-in, y la máquina de estados que dispara recordatorios y escalamiento.
/// </summary>
public class VerificacionVidaService : IVerificacionVidaService
{
    // Debe coincidir con "opcionesFrecuencia" en verificacion-vida.tsx del frontend.
    private static readonly int[] FrecuenciasValidas = [3, 6, 12];

    private readonly IConfiguracionVerificacionVidaRepository _configuracionRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IAsignacionHerenciaRepository _asignacionHerenciaRepository;
    private readonly ICertificadoDefuncionRepository _certificadoDefuncionRepository;
    private readonly INotificationService _notificationService;
    private readonly IConfiguration _configuration;

    public VerificacionVidaService(
        IConfiguracionVerificacionVidaRepository configuracionRepository,
        IUsuarioRepository usuarioRepository,
        IAsignacionHerenciaRepository asignacionHerenciaRepository,
        ICertificadoDefuncionRepository certificadoDefuncionRepository,
        INotificationService notificationService,
        IConfiguration configuration)
    {
        _configuracionRepository = configuracionRepository;
        _usuarioRepository = usuarioRepository;
        _asignacionHerenciaRepository = asignacionHerenciaRepository;
        _certificadoDefuncionRepository = certificadoDefuncionRepository;
        _notificationService = notificationService;
        _configuration = configuration;
    }

    /// <summary>
    /// Devuelve la configuración de verificación de vida de un usuario, o valores por
    /// defecto (sin persistir) si todavía no configuró nada.
    /// </summary>
    public async Task<ConfiguracionVerificacionVidaDTO> ObtenerConfiguracionAsync(int usuarioId)
    {
        try
        {
            var configuracion = await _configuracionRepository.ObtenerPorUsuarioIdAsync(usuarioId);

            // A diferencia del resto del proyecto, "todavía no existe configuración" no es
            // un 404: es el estado inicial de cualquier titular que nunca abrió esta pantalla.
            if (configuracion is null)
            {
                return new ConfiguracionVerificacionVidaDTO
                {
                    UsuarioId = usuarioId,
                    Activo = false,
                    FrecuenciaMeses = 3,
                    Metodo = MetodoNotificacion.Push,
                    UltimoCheckIn = DateTime.UtcNow,
                    Estado = EstadoVerificacionVida.Activo
                };
            }

            return MapearADTO(configuracion);
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al obtener la configuracion de verificacion de vida.", ex);
        }
    }

    /// <summary>
    /// Crea o actualiza la configuración de verificación de vida de un usuario.
    /// </summary>
    public async Task<ConfiguracionVerificacionVidaDTO> GuardarConfiguracionAsync(
        int usuarioId, ConfiguracionVerificacionVidaActualizacionDTO configuracionDTO)
    {
        if (!FrecuenciasValidas.Contains(configuracionDTO.FrecuenciaMeses))
        {
            throw new ReglaNegocioException("La frecuencia de chequeo debe ser 3, 6 o 12 meses.");
        }

        try
        {
            // Si se activa el monitoreo, el contacto de confianza es obligatorio y debe ser
            // un beneficiario ya ACEPTADO de algún activo de este titular.
            if (configuracionDTO.Activo)
            {
                if (configuracionDTO.ContactoConfianzaId is null)
                {
                    throw new ReglaNegocioException(
                        "Debe configurar un contacto de confianza antes de activar el monitoreo.");
                }

                var contacto = await _usuarioRepository.ObtenerPorIdAsync(configuracionDTO.ContactoConfianzaId.Value);

                if (contacto is null)
                {
                    throw new RecursoNoEncontradoException(
                        $"No se encontro el usuario con Id {configuracionDTO.ContactoConfianzaId.Value}.");
                }

                var herederosAceptados = await _asignacionHerenciaRepository.ObtenerAceptadasPorOtorganteAsync(usuarioId);
                var esContactoValido = herederosAceptados.Any(a => a.UsuarioId == configuracionDTO.ContactoConfianzaId.Value);

                if (!esContactoValido)
                {
                    throw new ReglaNegocioException(
                        "El contacto de confianza debe ser un beneficiario que ya haya aceptado la invitacion a heredar algun activo suyo.");
                }
            }

            var configuracion = await _configuracionRepository.ObtenerPorUsuarioIdAsync(usuarioId);
            var ahora = DateTime.UtcNow;

            if (configuracion is null)
            {
                configuracion = new ConfiguracionVerificacionVida
                {
                    UsuarioId = usuarioId,
                    Activo = configuracionDTO.Activo,
                    FrecuenciaMeses = configuracionDTO.FrecuenciaMeses,
                    Metodo = configuracionDTO.Metodo,
                    ContactoConfianzaId = configuracionDTO.ContactoConfianzaId,
                    // El alta cuenta como primer check-in implícito, para que el titular no
                    // aparezca "vencido" apenas configurado.
                    UltimoCheckIn = ahora,
                    Estado = EstadoVerificacionVida.Activo,
                    FechaCreacion = ahora,
                    UsuarioCreacion = "sistema"
                };

                await _configuracionRepository.AgregarAsync(configuracion);
            }
            else
            {
                // Reactivación: se trata como check-in nuevo para no aparecer "vencido" de entrada.
                var seEstaReactivando = !configuracion.Activo && configuracionDTO.Activo;

                configuracion.Activo = configuracionDTO.Activo;
                configuracion.FrecuenciaMeses = configuracionDTO.FrecuenciaMeses;
                configuracion.Metodo = configuracionDTO.Metodo;
                configuracion.ContactoConfianzaId = configuracionDTO.ContactoConfianzaId;

                if (seEstaReactivando)
                {
                    configuracion.UltimoCheckIn = ahora;
                    configuracion.Estado = EstadoVerificacionVida.Activo;
                    configuracion.RecordatoriosEnviados = 0;
                    configuracion.FechaUltimoRecordatorio = null;
                    configuracion.FechaProtocoloActivado = null;
                }

                configuracion.FechaModificacion = ahora;
                configuracion.UsuarioModificacion = "sistema";

                await _configuracionRepository.ActualizarAsync(configuracion);
            }

            return MapearADTO(configuracion);
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
            throw new ReglaNegocioException("Ocurrio un error al guardar la configuracion de verificacion de vida.", ex);
        }
    }

    /// <summary>
    /// Registra un check-in de actividad del titular, reiniciando el reloj de vencimiento
    /// y cancelando cualquier certificado de defunción pendiente subido por error.
    /// </summary>
    public async Task<ConfiguracionVerificacionVidaDTO> RegistrarCheckInAsync(int usuarioId)
    {
        try
        {
            var configuracion = await _configuracionRepository.ObtenerPorUsuarioIdAsync(usuarioId);

            if (configuracion is null)
            {
                throw new RecursoNoEncontradoException(
                    "No tiene configurado el monitoreo de verificacion de vida.");
            }

            var ahora = DateTime.UtcNow;

            configuracion.UltimoCheckIn = ahora;
            configuracion.Estado = EstadoVerificacionVida.Activo;
            configuracion.RecordatoriosEnviados = 0;
            configuracion.FechaUltimoRecordatorio = null;
            configuracion.FechaProtocoloActivado = null;
            configuracion.FechaModificacion = ahora;
            configuracion.UsuarioModificacion = "sistema";

            await _configuracionRepository.ActualizarAsync(configuracion);

            // Un certificado pendiente subido por error queda invalidado (nunca se borra,
            // se marca CanceladoPorActividad para dejar registro de la falsa alarma).
            var certificadosPendientes = await _certificadoDefuncionRepository.ObtenerPendientesPorTitularAsync(usuarioId);

            foreach (var certificado in certificadosPendientes)
            {
                certificado.Estado = EstadoCertificadoDefuncion.CanceladoPorActividad;
                certificado.MotivoRechazo = "Cancelado automaticamente: el titular confirmo actividad.";
                certificado.FechaRevision = ahora;
                certificado.FechaModificacion = ahora;
                certificado.UsuarioModificacion = "sistema";

                await _certificadoDefuncionRepository.ActualizarAsync(certificado);
            }

            return MapearADTO(configuracion);
        }
        catch (RecursoNoEncontradoException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al registrar el check-in de verificacion de vida.", ex);
        }
    }

    /// <summary>
    /// Recorre las configuraciones activas y avanza la máquina de estados de cada una:
    /// envía recordatorios ante inactividad y, agotado el plazo, activa el protocolo de
    /// escalamiento notificando a los herederos.
    /// </summary>
    public async Task EjecutarEscaneoAsync()
    {
        // Umbrales configurables (VerificacionVida:* en appsettings.json).
        var diasEntreRecordatorios = LeerEnteroDeConfiguracion("VerificacionVida:DiasEntreRecordatorios", 7);
        var cantidadRecordatorios = LeerEnteroDeConfiguracion("VerificacionVida:CantidadRecordatorios", 3);
        var diasPlazoFinal = LeerEnteroDeConfiguracion("VerificacionVida:DiasPlazoFinalTrasUltimoRecordatorio", 30);

        var ahora = DateTime.UtcNow;
        var configuracionesActivas = await _configuracionRepository.ObtenerActivasParaEscaneoAsync();

        foreach (var configuracion in configuracionesActivas)
        {
            var vencimiento = configuracion.UltimoCheckIn.AddMonths(configuracion.FrecuenciaMeses);

            if (ahora < vencimiento)
            {
                continue;
            }

            if (configuracion.RecordatoriosEnviados < cantidadRecordatorios)
            {
                // El primer recordatorio se dispara apenas vence el plazo; los siguientes,
                // cada "diasEntreRecordatorios" desde el último enviado.
                var tocaEnviarRecordatorio = configuracion.RecordatoriosEnviados == 0
                    || ahora >= configuracion.FechaUltimoRecordatorio!.Value.AddDays(diasEntreRecordatorios);

                if (!tocaEnviarRecordatorio)
                {
                    continue;
                }

                await _notificationService.EnviarNotificacionAsync(
                    configuracion.Usuario,
                    configuracion.Metodo,
                    "Confirmanos que estas bien",
                    $"No registramos actividad desde {configuracion.UltimoCheckIn:d}. " +
                    "Ingresa a la app y confirma tu check-in para que todo siga como esta.");

                configuracion.RecordatoriosEnviados++;
                configuracion.FechaUltimoRecordatorio = ahora;
                configuracion.Estado = EstadoVerificacionVida.RecordatorioEnviado;
                configuracion.FechaModificacion = ahora;
                configuracion.UsuarioModificacion = "sistema";

                await _configuracionRepository.ActualizarAsync(configuracion);

                // Se avisa al contacto de confianza en el primer recordatorio, apenas se
                // detecta la falta de respuesta (no recién al activarse el protocolo).
                if (configuracion.RecordatoriosEnviados == 1 && configuracion.ContactoConfianza is not null)
                {
                    await _notificationService.EnviarNotificacionAsync(
                        configuracion.ContactoConfianza,
                        MetodoNotificacion.Email,
                        $"{configuracion.Usuario.Nombre} no confirmo actividad",
                        $"Como contacto de confianza de {configuracion.Usuario.Nombre}, te avisamos que todavia " +
                        "no respondio a su verificacion de vida periodica. Te mantendremos informado.");
                }

                continue;
            }

            var seCumplioElPlazoFinal = ahora >= configuracion.FechaUltimoRecordatorio!.Value.AddDays(diasPlazoFinal);

            if (!seCumplioElPlazoFinal || configuracion.Estado == EstadoVerificacionVida.EsperandoCertificado)
            {
                continue;
            }

            configuracion.Estado = EstadoVerificacionVida.EsperandoCertificado;
            configuracion.FechaProtocoloActivado = ahora;
            configuracion.FechaModificacion = ahora;
            configuracion.UsuarioModificacion = "sistema";

            await _configuracionRepository.ActualizarAsync(configuracion);

            var herederosAceptados = await _asignacionHerenciaRepository.ObtenerAceptadasPorOtorganteAsync(configuracion.UsuarioId);

            foreach (var herencia in herederosAceptados)
            {
                if (herencia.Usuario is null)
                {
                    continue;
                }

                await _notificationService.EnviarNotificacionAsync(
                    herencia.Usuario,
                    MetodoNotificacion.Email,
                    $"Se requiere el certificado de defuncion de {configuracion.Usuario.Nombre}",
                    $"{configuracion.Usuario.Nombre} no confirmo actividad durante un periodo prolongado. " +
                    "Para continuar con el proceso de herencia, subi el certificado de defuncion desde la app.");
            }
        }
    }

    private int LeerEnteroDeConfiguracion(string clave, int valorPorDefecto)
    {
        var valorCrudo = _configuration[clave];
        return int.TryParse(valorCrudo, out var valor) ? valor : valorPorDefecto;
    }

    private static ConfiguracionVerificacionVidaDTO MapearADTO(ConfiguracionVerificacionVida configuracion)
    {
        return new ConfiguracionVerificacionVidaDTO
        {
            UsuarioId = configuracion.UsuarioId,
            Activo = configuracion.Activo,
            FrecuenciaMeses = configuracion.FrecuenciaMeses,
            Metodo = configuracion.Metodo,
            ContactoConfianzaId = configuracion.ContactoConfianzaId,
            ContactoConfianzaNombre = configuracion.ContactoConfianza?.Nombre,
            UltimoCheckIn = configuracion.UltimoCheckIn,
            Estado = configuracion.Estado,
            RecordatoriosEnviados = configuracion.RecordatoriosEnviados,
            FechaUltimoRecordatorio = configuracion.FechaUltimoRecordatorio,
            FechaProtocoloActivado = configuracion.FechaProtocoloActivado
        };
    }
}
