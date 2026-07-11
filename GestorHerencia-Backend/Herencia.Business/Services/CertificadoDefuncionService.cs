using Herencia.Business.Dtos;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Herencia.Data.Models;
using Herencia.Data.Repositories;
using Microsoft.Extensions.Configuration;

namespace Herencia.Business.Services;

// CertificadoDefuncionService implementa la logica de negocio de subida y
// revision de certificados de defuncion, incluida la liberacion de bienes
// que dispara una aprobacion.
public class CertificadoDefuncionService : ICertificadoDefuncionService
{
    // Tipos de archivo aceptados: documento (PDF) o foto/escaneo del acta
    // (JPG/PNG). Se valida el ContentType reportado por el cliente, no la
    // extension del nombre del archivo (que es trivial de falsificar).
    private static readonly string[] TiposPermitidos =
    [
        "application/pdf",
        "image/jpeg",
        "image/png"
    ];

    private readonly ICertificadoDefuncionRepository _certificadoDefuncionRepository;
    private readonly IConfiguracionVerificacionVidaRepository _configuracionRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IAsignacionHerenciaRepository _asignacionHerenciaRepository;
    private readonly IAlmacenamientoArchivosService _almacenamientoService;
    private readonly INotificationService _notificationService;
    private readonly IConfiguration _configuration;

    public CertificadoDefuncionService(
        ICertificadoDefuncionRepository certificadoDefuncionRepository,
        IConfiguracionVerificacionVidaRepository configuracionRepository,
        IUsuarioRepository usuarioRepository,
        IAsignacionHerenciaRepository asignacionHerenciaRepository,
        IAlmacenamientoArchivosService almacenamientoService,
        INotificationService notificationService,
        IConfiguration configuration)
    {
        _certificadoDefuncionRepository = certificadoDefuncionRepository;
        _configuracionRepository = configuracionRepository;
        _usuarioRepository = usuarioRepository;
        _asignacionHerenciaRepository = asignacionHerenciaRepository;
        _almacenamientoService = almacenamientoService;
        _notificationService = notificationService;
        _configuration = configuration;
    }

    public async Task<CertificadoDefuncionDTO> SubirCertificadoAsync(
        int usuarioTitularId,
        int subidoPorUsuarioId,
        Stream contenidoArchivo,
        string nombreArchivoOriginal,
        string contentType,
        long tamanioBytes)
    {
        // --- Paso 1: validaciones de formato, antes de tocar disco o base de datos ---
        if (!TiposPermitidos.Contains(contentType))
        {
            throw new ReglaNegocioException("Solo se aceptan archivos PDF, JPG o PNG.");
        }

        // Se lee con el indexador plano de IConfiguration (no GetValue<T>,
        // que exige el paquete Microsoft.Extensions.Configuration.Binder
        // que este proyecto no referencia) y se parsea a mano, igual
        // criterio que ya usa TokenService para "AppSettings:Token".
        var tamanioMaximoBytes = long.TryParse(
            _configuration["VerificacionVida:TamanioMaximoCertificadoBytes"], out var valorConfigurado)
            ? valorConfigurado
            : 10 * 1024 * 1024; // 10 MB por defecto

        if (tamanioBytes > tamanioMaximoBytes)
        {
            throw new ReglaNegocioException(
                $"El archivo supera el tamaño maximo permitido ({tamanioMaximoBytes / (1024 * 1024)} MB).");
        }

        try
        {
            var titular = await _usuarioRepository.ObtenerPorIdAsync(usuarioTitularId);

            if (titular is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el usuario con Id {usuarioTitularId}.");
            }

            // --- El fallecimiento de este titular ya fue confirmado antes: no se acepta otro ---
            // Sin este chequeo, cualquier heredero aceptado podia seguir subiendo
            // certificados indefinidamente para la misma persona, incluso despues
            // de que un Administrador ya hubiera aprobado uno y liberado los bienes.
            if (await _certificadoDefuncionRepository.ExisteCertificadoAprobadoAsync(usuarioTitularId))
            {
                throw new ReglaNegocioException(
                    "El fallecimiento de este titular ya fue confirmado anteriormente; no se puede subir otro certificado.");
            }

            // --- Regla de negocio central: solo un heredero ACEPTADO puede subir esto ---
            // Cubre las DOS vias posibles de llegar aca (subida proactiva, o
            // subida pedida tras el escalamiento de VerificacionVidaService):
            // en ambos casos, quien sube el documento tiene que ser
            // efectivamente uno de los herederos de este titular, nunca un
            // tercero sin relacion con el.
            var herederosAceptados = await _asignacionHerenciaRepository.ObtenerAceptadasPorOtorganteAsync(usuarioTitularId);
            var heredero = herederosAceptados.FirstOrDefault(a => a.UsuarioId == subidoPorUsuarioId);

            if (heredero?.Usuario is null)
            {
                throw new ReglaNegocioException(
                    "Solo un heredero que ya acepto la invitacion puede subir el certificado de defuncion de este titular.");
            }

            var rutaGuardada = await _almacenamientoService.GuardarArchivoAsync(
                contenidoArchivo, nombreArchivoOriginal, subcarpeta: "certificados_defuncion");

            var certificado = new CertificadoDefuncion
            {
                UsuarioTitularId = usuarioTitularId,
                SubidoPorUsuarioId = subidoPorUsuarioId,
                RutaArchivo = rutaGuardada,
                NombreArchivoOriginal = nombreArchivoOriginal,
                Estado = EstadoCertificadoDefuncion.Pendiente,
                FechaCreacion = DateTime.UtcNow,
                UsuarioCreacion = "sistema"
            };

            await _certificadoDefuncionRepository.AgregarAsync(certificado);

            // Si el titular tenia monitoreo configurado, se refleja el
            // nuevo estado "hay un certificado en revision" (salvo que el
            // fallecimiento ya estuviera confirmado por otro certificado
            // previo, caso en el que no corresponde retroceder el estado).
            var configuracion = await _configuracionRepository.ObtenerPorUsuarioIdAsync(usuarioTitularId);

            if (configuracion is not null
                && configuracion.Estado != EstadoVerificacionVida.FallecimientoConfirmado
                && configuracion.Estado != EstadoVerificacionVida.HerenciaLiberada)
            {
                configuracion.Estado = EstadoVerificacionVida.CertificadoEnRevision;
                configuracion.FechaModificacion = DateTime.UtcNow;
                configuracion.UsuarioModificacion = "sistema";
                await _configuracionRepository.ActualizarAsync(configuracion);
            }

            return new CertificadoDefuncionDTO
            {
                Id = certificado.Id,
                UsuarioTitularId = titular.Id,
                UsuarioTitularNombre = titular.Nombre,
                SubidoPorUsuarioId = heredero.Usuario.Id,
                SubidoPorNombre = heredero.Usuario.Nombre,
                NombreArchivoOriginal = certificado.NombreArchivoOriginal,
                FechaSubida = certificado.FechaCreacion,
                Estado = certificado.Estado
            };
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
            throw new ReglaNegocioException("Ocurrio un error al subir el certificado de defuncion.", ex);
        }
    }

    public async Task<IEnumerable<CertificadoDefuncionDTO>> ObtenerPendientesAsync()
    {
        try
        {
            var pendientes = await _certificadoDefuncionRepository.ObtenerPendientesAsync();

            return pendientes.Select(MapearADTO);
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al obtener los certificados de defuncion pendientes.", ex);
        }
    }

    public async Task<CertificadoDefuncionDTO> AprobarAsync(int certificadoId, int adminUsuarioId)
    {
        try
        {
            var certificado = await _certificadoDefuncionRepository.ObtenerConUsuariosAsync(certificadoId);

            if (certificado is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el certificado de defuncion con Id {certificadoId}.");
            }

            // Misma regla "una vez decidido, decidido" que
            // AsignacionHerenciaService.CambiarEstadoInternoAsync aplica
            // sobre EstadoBeneficiario: un certificado ya revisado no puede
            // volver a aprobarse ni rechazarse.
            if (certificado.Estado != EstadoCertificadoDefuncion.Pendiente)
            {
                throw new ReglaNegocioException("Este certificado ya fue revisado y no puede modificarse.");
            }

            var ahora = DateTime.UtcNow;
            var herederosLiberados = Enumerable.Empty<AsignacionHerencia>();

            // --- Transaccion: aprobar el certificado + liberar TODOS los
            // bienes del titular deben confirmarse (o revertirse) juntos ---
            // Un fallo a mitad de camino (ej: al liberar la tercera de
            // cinco AsignacionHerencia) no puede dejar al certificado
            // marcado "Aprobado" con solo una liberacion parcial aplicada.
            await _certificadoDefuncionRepository.EjecutarEnTransaccionAsync(async () =>
            {
                certificado.Estado = EstadoCertificadoDefuncion.Aprobado;
                certificado.RevisadoPorUsuarioId = adminUsuarioId;
                certificado.FechaRevision = ahora;
                certificado.FechaModificacion = ahora;
                certificado.UsuarioModificacion = "sistema";
                await _certificadoDefuncionRepository.ActualizarAsync(certificado);

                var configuracion = await _configuracionRepository.ObtenerPorUsuarioIdAsync(certificado.UsuarioTitularId);

                if (configuracion is not null)
                {
                    configuracion.Estado = EstadoVerificacionVida.HerenciaLiberada;
                    configuracion.FechaModificacion = ahora;
                    configuracion.UsuarioModificacion = "sistema";
                    await _configuracionRepository.ActualizarAsync(configuracion);
                }

                herederosLiberados = await _asignacionHerenciaRepository.ObtenerAceptadasPorOtorganteAsync(certificado.UsuarioTitularId);

                foreach (var herencia in herederosLiberados)
                {
                    herencia.FechaLiberacion = ahora;
                    herencia.FechaModificacion = ahora;
                    herencia.UsuarioModificacion = "sistema";
                    await _asignacionHerenciaRepository.ActualizarAsync(herencia);
                }
            });

            // --- Notificaciones, DESPUES de confirmada la transaccion ---
            // Si el envio de una notificacion fallara, no tiene sentido
            // revertir la liberacion de bienes ya confirmada: por eso vive
            // fuera del bloque transaccional.
            foreach (var herencia in herederosLiberados)
            {
                if (herencia.Usuario is null)
                {
                    continue;
                }

                await _notificationService.EnviarNotificacionAsync(
                    herencia.Usuario,
                    MetodoNotificacion.Email,
                    $"Se liberaron los bienes de {certificado.UsuarioTitular.Nombre}",
                    "El fallecimiento fue confirmado y los activos digitales que te correspondian ya estan disponibles en la app.");
            }

            return MapearADTO(certificado);
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
            throw new ReglaNegocioException("Ocurrio un error al aprobar el certificado de defuncion.", ex);
        }
    }

    public async Task<CertificadoDefuncionDTO> RechazarAsync(int certificadoId, int adminUsuarioId, string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new ReglaNegocioException("El motivo del rechazo es obligatorio.");
        }

        try
        {
            var certificado = await _certificadoDefuncionRepository.ObtenerConUsuariosAsync(certificadoId);

            if (certificado is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el certificado de defuncion con Id {certificadoId}.");
            }

            if (certificado.Estado != EstadoCertificadoDefuncion.Pendiente)
            {
                throw new ReglaNegocioException("Este certificado ya fue revisado y no puede modificarse.");
            }

            certificado.Estado = EstadoCertificadoDefuncion.Rechazado;
            certificado.RevisadoPorUsuarioId = adminUsuarioId;
            certificado.FechaRevision = DateTime.UtcNow;
            certificado.MotivoRechazo = motivo.Trim();
            certificado.FechaModificacion = DateTime.UtcNow;
            certificado.UsuarioModificacion = "sistema";

            // Deliberadamente NO se toca ConfiguracionVerificacionVida.Estado
            // aca: un rechazo no confirma nada, asi que otro heredero
            // (o el mismo) puede volver a subir un certificado distinto sin
            // que el sistema quede en un estado inconsistente.
            await _certificadoDefuncionRepository.ActualizarAsync(certificado);

            return MapearADTO(certificado);
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
            throw new ReglaNegocioException("Ocurrio un error al rechazar el certificado de defuncion.", ex);
        }
    }

    public async Task<(string RutaArchivo, string NombreArchivoOriginal)> ObtenerArchivoAsync(int certificadoId)
    {
        var certificado = await _certificadoDefuncionRepository.ObtenerConUsuariosAsync(certificadoId);

        if (certificado is null)
        {
            throw new RecursoNoEncontradoException($"No se encontro el certificado de defuncion con Id {certificadoId}.");
        }

        return (certificado.RutaArchivo, certificado.NombreArchivoOriginal);
    }

    private static CertificadoDefuncionDTO MapearADTO(CertificadoDefuncion certificado)
    {
        return new CertificadoDefuncionDTO
        {
            Id = certificado.Id,
            UsuarioTitularId = certificado.UsuarioTitularId,
            UsuarioTitularNombre = certificado.UsuarioTitular?.Nombre ?? string.Empty,
            SubidoPorUsuarioId = certificado.SubidoPorUsuarioId,
            SubidoPorNombre = certificado.SubidoPor?.Nombre ?? string.Empty,
            NombreArchivoOriginal = certificado.NombreArchivoOriginal,
            FechaSubida = certificado.FechaCreacion,
            Estado = certificado.Estado,
            RevisadoPorUsuarioId = certificado.RevisadoPorUsuarioId,
            FechaRevision = certificado.FechaRevision,
            MotivoRechazo = certificado.MotivoRechazo
        };
    }
}
