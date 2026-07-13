using Herencia.Business.Dtos;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Herencia.Data.Models;
using Herencia.Data.Repositories;
using Microsoft.Extensions.Configuration;

namespace Herencia.Business.Services;

/// <summary>
/// Implementación de <see cref="IActivoDigitalService"/>: valida que el usuario titular
/// exista antes de asignarle un activo y traduce errores técnicos a excepciones de negocio.
/// </summary>
public class ActivoDigitalService : IActivoDigitalService
{
    private readonly IActivoDigitalRepository _activoDigitalRepository;

    // Usado para validar que el usuario titular exista antes de crearle un activo.
    private readonly IUsuarioRepository _usuarioRepository;

    private readonly IAlmacenamientoArchivosService _almacenamientoService;
    private readonly IConfiguration _configuration;

    // Se valida el ContentType, no la extensión del nombre (trivial de falsificar).
    private static readonly string[] TiposPermitidos =
    [
        "application/pdf",
        "image/jpeg",
        "image/png"
    ];

    public ActivoDigitalService(
        IActivoDigitalRepository activoDigitalRepository,
        IUsuarioRepository usuarioRepository,
        IAlmacenamientoArchivosService almacenamientoService,
        IConfiguration configuration)
    {
        _activoDigitalRepository = activoDigitalRepository;
        _usuarioRepository = usuarioRepository;
        _almacenamientoService = almacenamientoService;
        _configuration = configuration;
    }

    /// <summary>
    /// Da de alta un nuevo activo digital, validando que el usuario titular exista.
    /// </summary>
    public async Task<ActivoDigitalDTO> CrearActivoDigitalAsync(ActivoDigitalCreacionDTO activoDigitalCreacionDTO)
    {
        if (string.IsNullOrWhiteSpace(activoDigitalCreacionDTO.Nombre))
        {
            throw new ReglaNegocioException("El nombre del activo digital no puede estar vacio.");
        }

        try
        {
            var usuarioTitular = await _usuarioRepository.ObtenerPorIdAsync(activoDigitalCreacionDTO.UsuarioId);

            if (usuarioTitular is null)
            {
                throw new RecursoNoEncontradoException(
                    $"No se puede crear el activo digital: el usuario titular con Id {activoDigitalCreacionDTO.UsuarioId} no existe.");
            }

            var activoDigital = new ActivoDigital
            {
                Nombre = activoDigitalCreacionDTO.Nombre.Trim(),
                Tipo = activoDigitalCreacionDTO.Tipo,
                Descripcion = activoDigitalCreacionDTO.Descripcion?.Trim() ?? string.Empty,
                UsuarioId = activoDigitalCreacionDTO.UsuarioId,
                FechaCreacion = DateTime.UtcNow,
                UsuarioCreacion = "sistema"
            };

            await _activoDigitalRepository.AgregarAsync(activoDigital);

            return MapearADTO(activoDigital);
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
            // El detalle técnico (ex) no se expone al llamador, solo queda como InnerException.
            throw new ReglaNegocioException("Ocurrio un error al procesar el activo digital.", ex);
        }
    }

    /// <summary>
    /// Busca un activo digital por Id.
    /// </summary>
    public async Task<ActivoDigitalDTO> ObtenerActivoDigitalPorIdAsync(int id)
    {
        try
        {
            var activoDigital = await _activoDigitalRepository.ObtenerPorIdAsync(id);

            if (activoDigital is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el activo digital con Id {id}.");
            }

            return MapearADTO(activoDigital);
        }
        catch (RecursoNoEncontradoException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al obtener el activo digital.", ex);
        }
    }

    /// <summary>
    /// Devuelve todos los activos digitales de un usuario, validando que exista.
    /// </summary>
    public async Task<IEnumerable<ActivoDigitalDTO>> ObtenerActivosPorUsuarioAsync(int usuarioId)
    {
        try
        {
            var usuarioTitular = await _usuarioRepository.ObtenerPorIdAsync(usuarioId);

            if (usuarioTitular is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el usuario con Id {usuarioId}.");
            }

            var activos = await _activoDigitalRepository.ObtenerActivosPorUsuarioAsync(usuarioId);

            return activos.Select(MapearADTO);
        }
        catch (RecursoNoEncontradoException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al obtener los activos digitales del usuario.", ex);
        }
    }

    /// <summary>
    /// Actualiza nombre, tipo y descripción de un activo existente. El usuario titular
    /// nunca se modifica por este medio: no permite "transferir" el activo.
    /// </summary>
    public async Task<ActivoDigitalDTO> ActualizarActivoDigitalAsync(int id, ActivoDigitalActualizacionDTO activoDigitalActualizacionDTO)
    {
        if (string.IsNullOrWhiteSpace(activoDigitalActualizacionDTO.Nombre))
        {
            throw new ReglaNegocioException("El nombre del activo digital no puede estar vacio.");
        }

        try
        {
            var activoDigital = await _activoDigitalRepository.ObtenerPorIdAsync(id);

            if (activoDigital is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el activo digital con Id {id}.");
            }

            activoDigital.Nombre = activoDigitalActualizacionDTO.Nombre.Trim();
            activoDigital.Tipo = activoDigitalActualizacionDTO.Tipo;
            activoDigital.Descripcion = activoDigitalActualizacionDTO.Descripcion?.Trim() ?? string.Empty;

            activoDigital.FechaModificacion = DateTime.UtcNow;
            activoDigital.UsuarioModificacion = "sistema";

            await _activoDigitalRepository.ActualizarAsync(activoDigital);

            return MapearADTO(activoDigital);
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
            throw new ReglaNegocioException("Ocurrio un error al actualizar el activo digital.", ex);
        }
    }

    /// <summary>
    /// Elimina un activo digital existente por su Id.
    /// </summary>
    public async Task EliminarActivoDigitalAsync(int id)
    {
        try
        {
            var activoDigital = await _activoDigitalRepository.ObtenerPorIdAsync(id);

            if (activoDigital is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el activo digital con Id {id}.");
            }

            // Cascade en AppDbContext elimina también las asignaciones que dependan de este activo.
            await _activoDigitalRepository.EliminarAsync(id);
        }
        catch (RecursoNoEncontradoException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al eliminar el activo digital.", ex);
        }
    }

    /// <summary>
    /// Devuelve una página de los activos digitales de un usuario junto con los
    /// metadatos necesarios para armar un paginador.
    /// </summary>
    public async Task<ResultadoPaginadoDTO<ActivoDigitalDTO>> ObtenerActivosPorUsuarioPaginadoAsync(
        int usuarioId, int pagina, int limite, TipoActivoDigital? tipo, string? nombre)
    {
        // Normalización defensiva: pagina/limite llegan de un query string HTTP no confiable.
        if (pagina < 1)
        {
            pagina = 1;
        }

        if (limite < 1)
        {
            limite = 10;
        }
        else if (limite > 100)
        {
            limite = 100;
        }

        try
        {
            var usuarioTitular = await _usuarioRepository.ObtenerPorIdAsync(usuarioId);

            if (usuarioTitular is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el usuario con Id {usuarioId}.");
            }

            var (items, total) = await _activoDigitalRepository.ObtenerActivosPorUsuarioPaginadoAsync(usuarioId, pagina, limite, tipo, nombre);

            return new ResultadoPaginadoDTO<ActivoDigitalDTO>
            {
                Items = items.Select(MapearADTO),
                PaginaActual = pagina,
                RegistrosPorPagina = limite,
                TotalRegistros = total,
                // Cast a double antes de dividir para que Math.Ceiling cubra la última página.
                TotalPaginas = (int)Math.Ceiling(total / (double)limite)
            };
        }
        catch (RecursoNoEncontradoException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al obtener los activos digitales paginados del usuario.", ex);
        }
    }

    /// <summary>
    /// Adjunta (o reemplaza) el archivo de un activo digital ya existente.
    /// </summary>
    public async Task<ActivoDigitalDTO> SubirArchivoAsync(
        int id, Stream contenido, string nombreArchivoOriginal, string contentType, long tamanioBytes)
    {
        if (!TiposPermitidos.Contains(contentType))
        {
            throw new ReglaNegocioException("Solo se aceptan archivos PDF, JPG o PNG.");
        }

        var tamanioMaximoBytes = long.TryParse(
            _configuration["VerificacionVida:TamanioMaximoCertificadoBytes"], out var valorConfigurado)
            ? valorConfigurado
            : 10 * 1024 * 1024;

        if (tamanioBytes > tamanioMaximoBytes)
        {
            throw new ReglaNegocioException(
                $"El archivo supera el tamaño maximo permitido ({tamanioMaximoBytes / (1024 * 1024)} MB).");
        }

        try
        {
            var activoDigital = await _activoDigitalRepository.ObtenerPorIdAsync(id);

            if (activoDigital is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el activo digital con Id {id}.");
            }

            var rutaGuardada = await _almacenamientoService.GuardarArchivoAsync(
                contenido, nombreArchivoOriginal, subcarpeta: "activos_digitales");

            activoDigital.RutaArchivo = rutaGuardada;
            activoDigital.NombreArchivoOriginal = nombreArchivoOriginal;
            activoDigital.FechaModificacion = DateTime.UtcNow;
            activoDigital.UsuarioModificacion = "sistema";

            await _activoDigitalRepository.ActualizarAsync(activoDigital);

            return MapearADTO(activoDigital);
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
            throw new ReglaNegocioException("Ocurrio un error al subir el archivo del activo digital.", ex);
        }
    }

    /// <summary>
    /// Devuelve la ruta física y el nombre original del archivo adjunto de un activo digital.
    /// </summary>
    public async Task<(string RutaArchivo, string NombreArchivoOriginal)> ObtenerArchivoAsync(int id)
    {
        var activoDigital = await _activoDigitalRepository.ObtenerPorIdAsync(id);

        if (activoDigital is null)
        {
            throw new RecursoNoEncontradoException($"No se encontro el activo digital con Id {id}.");
        }

        return (activoDigital.RutaArchivo ?? string.Empty, activoDigital.NombreArchivoOriginal ?? string.Empty);
    }

    private static ActivoDigitalDTO MapearADTO(ActivoDigital activoDigital)
    {
        return new ActivoDigitalDTO
        {
            Id = activoDigital.Id,
            Nombre = activoDigital.Nombre,
            Tipo = activoDigital.Tipo,
            Descripcion = activoDigital.Descripcion,
            UsuarioId = activoDigital.UsuarioId,
            NombreArchivoOriginal = activoDigital.NombreArchivoOriginal
        };
    }
}
