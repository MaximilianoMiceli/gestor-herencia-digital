using Herencia.Business.Dtos;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Herencia.Data.Models;
using Herencia.Data.Repositories;
using Microsoft.Extensions.Configuration;

namespace Herencia.Business.Services;

// VerificacionVidaService implementa la logica de negocio del monitoreo de
// actividad: configuracion, check-in, y la maquina de estados que dispara
// recordatorios y escalamiento (EjecutarEscaneoAsync).
public class VerificacionVidaService : IVerificacionVidaService
{
    // Las frecuencias validas de chequeo, identicas a "opcionesFrecuencia"
    // en verificacion-vida.tsx del frontend.
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

    public async Task<ConfiguracionVerificacionVidaDTO> ObtenerConfiguracionAsync(int usuarioId)
    {
        try
        {
            var configuracion = await _configuracionRepository.ObtenerPorUsuarioIdAsync(usuarioId);

            // A diferencia del resto del proyecto, "todavia no existe
            // configuracion" NO es un 404: es, literalmente, el estado
            // inicial de cualquier titular que nunca abrio esta pantalla.
            // Se devuelve un DTO con valores por defecto (sin persistir
            // nada) para que el cliente pueda renderizar el formulario
            // vacio sin tener que distinguir "error" de "no configurado".
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

    public async Task<ConfiguracionVerificacionVidaDTO> GuardarConfiguracionAsync(
        int usuarioId, ConfiguracionVerificacionVidaActualizacionDTO configuracionDTO)
    {
        // --- Paso 1: validacion de formato, antes de tocar la base de datos ---
        if (!FrecuenciasValidas.Contains(configuracionDTO.FrecuenciaMeses))
        {
            throw new ReglaNegocioException("La frecuencia de chequeo debe ser 3, 6 o 12 meses.");
        }

        try
        {
            // --- Paso 2: si se pide ACTIVAR el monitoreo, el contacto de
            // confianza es obligatorio y debe ser un beneficiario ya
            // ACEPTADO de algun activo de ESTE titular (misma regla que ya
            // aplica verificacion-vida.tsx del lado del cliente; se repite
            // aca porque el cliente NO es una fuente confiable: cualquiera
            // podria llamar a este endpoint saltandose la validacion de la
            // app). ---
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

            // --- Paso 3: crear o actualizar ---
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
                    // Alta = primer check-in implicito: no tendria sentido
                    // que, apenas configurado, el titular ya aparezca
                    // "vencido" por una fecha en blanco.
                    UltimoCheckIn = ahora,
                    Estado = EstadoVerificacionVida.Activo,
                    FechaCreacion = ahora,
                    UsuarioCreacion = "sistema"
                };

                await _configuracionRepository.AgregarAsync(configuracion);
            }
            else
            {
                // --- Reactivacion: reiniciar el reloj ---
                // Si el monitoreo estaba desactivado y esta edicion lo
                // vuelve a activar, se trata como un check-in nuevo: de lo
                // contrario, un titular que reactiva el monitoreo despues
                // de mucho tiempo inactivo apareceria "vencido" desde el
                // primer escaneo, sin haber tenido chance de responder.
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

            // --- Cancelacion automatica de un pedido de certificado en curso ---
            // Si el titular vuelve a confirmar actividad mientras algun
            // heredero ya habia subido un certificado (Pendiente de
            // revision), ese pedido queda invalidado: se marca
            // CanceladoPorActividad (NUNCA se borra la fila, para dejar
            // registro de que existio un pedido que resulto ser una falsa
            // alarma).
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

    public async Task EjecutarEscaneoAsync()
    {
        // --- Umbrales configurables (VerificacionVida:* en appsettings.json) ---
        // Con los valores por defecto acordados: 3 recordatorios espaciados
        // cada 7 dias tras el vencimiento, y un plazo final de 30 dias
        // contados desde el ULTIMO recordatorio antes de activar el
        // protocolo.
        // Se lee con el indexador plano de IConfiguration (no GetValue<T>,
        // que exige el paquete Microsoft.Extensions.Configuration.Binder
        // que este proyecto no referencia) y se parsea a mano, igual
        // criterio que ya usa TokenService para "AppSettings:Token".
        var diasEntreRecordatorios = LeerEnteroDeConfiguracion("VerificacionVida:DiasEntreRecordatorios", 7);
        var cantidadRecordatorios = LeerEnteroDeConfiguracion("VerificacionVida:CantidadRecordatorios", 3);
        var diasPlazoFinal = LeerEnteroDeConfiguracion("VerificacionVida:DiasPlazoFinalTrasUltimoRecordatorio", 30);

        var ahora = DateTime.UtcNow;
        var configuracionesActivas = await _configuracionRepository.ObtenerActivasParaEscaneoAsync();

        foreach (var configuracion in configuracionesActivas)
        {
            var vencimiento = configuracion.UltimoCheckIn.AddMonths(configuracion.FrecuenciaMeses);

            // Todavia no vencio el plazo: nada que hacer con este titular
            // en este tick.
            if (ahora < vencimiento)
            {
                continue;
            }

            if (configuracion.RecordatoriosEnviados < cantidadRecordatorios)
            {
                // El PRIMER recordatorio se dispara apenas vence el plazo;
                // los siguientes, cada "diasEntreRecordatorios" desde el
                // ULTIMO enviado.
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

                // --- Aviso al contacto de confianza en el PRIMER recordatorio ---
                // Cumple lo que ya le promete verificacion-vida.tsx al
                // titular ("tu contacto de confianza sera notificado antes
                // de activar la herencia"): se avisa apenas se detecta la
                // falta de respuesta, no recien al activarse el protocolo.
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

            // Ya se agotaron los recordatorios: falta evaluar el plazo
            // final de "diasPlazoFinal" contado desde el ULTIMO recordatorio.
            var seCumplioElPlazoFinal = ahora >= configuracion.FechaUltimoRecordatorio!.Value.AddDays(diasPlazoFinal);

            if (!seCumplioElPlazoFinal || configuracion.Estado == EstadoVerificacionVida.EsperandoCertificado)
            {
                continue;
            }

            // --- Activacion del protocolo ---
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
