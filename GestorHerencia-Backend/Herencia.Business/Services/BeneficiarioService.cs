using System.ComponentModel.DataAnnotations;
using Herencia.Business.Dtos;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Herencia.Data.Models;
using Herencia.Data.Repositories;

namespace Herencia.Business.Services;

// BeneficiarioService es la implementacion CONCRETA de IBeneficiarioService,
// siguiendo el mismo patron ya usado por ActivoDigitalService: depende de DOS
// repositorios (Beneficiario y Usuario) porque su regla de negocio central
// ("el usuario titular debe existir") involucra a ambas entidades.
public class BeneficiarioService : IBeneficiarioService
{
    private readonly IBeneficiarioRepository _beneficiarioRepository;
    private readonly IUsuarioRepository _usuarioRepository;

    public BeneficiarioService(
        IBeneficiarioRepository beneficiarioRepository,
        IUsuarioRepository usuarioRepository)
    {
        _beneficiarioRepository = beneficiarioRepository;
        _usuarioRepository = usuarioRepository;
    }

    // CrearBeneficiarioAsync: da de alta un nuevo Beneficiario, validando
    // primero que el Usuario titular exista.
    public async Task<BeneficiarioDTO> CrearBeneficiarioAsync(BeneficiarioCreacionDTO beneficiarioCreacionDTO)
    {
        if (string.IsNullOrWhiteSpace(beneficiarioCreacionDTO.Nombre))
        {
            throw new ReglaNegocioException("El nombre del beneficiario no puede estar vacio.");
        }

        // Validacion de formato de Email, con el mismo criterio (regex
        // simple, no 100% RFC-compliant) que ya usa UsuarioService.
        if (string.IsNullOrWhiteSpace(beneficiarioCreacionDTO.Email) ||
            !new EmailAddressAttribute().IsValid(beneficiarioCreacionDTO.Email))
        {
            throw new ReglaNegocioException("El email del beneficiario no tiene un formato valido.");
        }

        try
        {
            // Regla de negocio: el usuario titular debe existir antes de
            // poder registrarle un beneficiario (misma logica que
            // ActivoDigitalService.CrearActivoDigitalAsync).
            var usuarioTitular = await _usuarioRepository.ObtenerPorIdAsync(beneficiarioCreacionDTO.UsuarioId);

            if (usuarioTitular is null)
            {
                throw new RecursoNoEncontradoException(
                    $"No se puede crear el beneficiario: el usuario titular con Id {beneficiarioCreacionDTO.UsuarioId} no existe.");
            }

            var beneficiario = new Beneficiario
            {
                Nombre = beneficiarioCreacionDTO.Nombre.Trim(),
                Email = beneficiarioCreacionDTO.Email.Trim(),
                Parentesco = beneficiarioCreacionDTO.Parentesco?.Trim() ?? string.Empty,
                UsuarioId = beneficiarioCreacionDTO.UsuarioId,
                FechaCreacion = DateTime.UtcNow,
                UsuarioCreacion = "sistema"
            };

            await _beneficiarioRepository.AgregarAsync(beneficiario);

            return MapearADTO(beneficiario);
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
            throw new ReglaNegocioException("Ocurrio un error al procesar el beneficiario.", ex);
        }
    }

    // ObtenerBeneficiarioPorIdAsync: busca un unico Beneficiario por su Id.
    public async Task<BeneficiarioDTO> ObtenerBeneficiarioPorIdAsync(int id)
    {
        try
        {
            var beneficiario = await _beneficiarioRepository.ObtenerPorIdAsync(id);

            if (beneficiario is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el beneficiario con Id {id}.");
            }

            return MapearADTO(beneficiario);
        }
        catch (RecursoNoEncontradoException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al obtener el beneficiario.", ex);
        }
    }

    // ObtenerBeneficiariosPorUsuarioAsync: lista los Beneficiarios de un
    // Usuario puntual, validando primero que ese Usuario exista.
    public async Task<IEnumerable<BeneficiarioDTO>> ObtenerBeneficiariosPorUsuarioAsync(int usuarioId)
    {
        try
        {
            var usuarioTitular = await _usuarioRepository.ObtenerPorIdAsync(usuarioId);

            if (usuarioTitular is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el usuario con Id {usuarioId}.");
            }

            var beneficiarios = await _beneficiarioRepository.ObtenerBeneficiariosPorUsuarioAsync(usuarioId);

            return beneficiarios.Select(MapearADTO);
        }
        catch (RecursoNoEncontradoException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException("Ocurrio un error al obtener los beneficiarios del usuario.", ex);
        }
    }

    // ActualizarBeneficiarioAsync: modifica Nombre, Email y Parentesco de un
    // Beneficiario existente.
    public async Task<BeneficiarioDTO> ActualizarBeneficiarioAsync(int id, BeneficiarioActualizacionDTO beneficiarioActualizacionDTO)
    {
        if (string.IsNullOrWhiteSpace(beneficiarioActualizacionDTO.Nombre))
        {
            throw new ReglaNegocioException("El nombre del beneficiario no puede estar vacio.");
        }

        if (string.IsNullOrWhiteSpace(beneficiarioActualizacionDTO.Email) ||
            !new EmailAddressAttribute().IsValid(beneficiarioActualizacionDTO.Email))
        {
            throw new ReglaNegocioException("El email del beneficiario no tiene un formato valido.");
        }

        try
        {
            var beneficiario = await _beneficiarioRepository.ObtenerPorIdAsync(id);

            if (beneficiario is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el beneficiario con Id {id}.");
            }

            beneficiario.Nombre = beneficiarioActualizacionDTO.Nombre.Trim();
            beneficiario.Email = beneficiarioActualizacionDTO.Email.Trim();
            beneficiario.Parentesco = beneficiarioActualizacionDTO.Parentesco?.Trim() ?? string.Empty;
            beneficiario.FechaModificacion = DateTime.UtcNow;
            beneficiario.UsuarioModificacion = "sistema";

            await _beneficiarioRepository.ActualizarAsync(beneficiario);

            return MapearADTO(beneficiario);
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
            throw new ReglaNegocioException("Ocurrio un error al actualizar el beneficiario.", ex);
        }
    }

    // EliminarBeneficiarioAsync: borra un Beneficiario existente por su Id.
    public async Task EliminarBeneficiarioAsync(int id)
    {
        try
        {
            var beneficiario = await _beneficiarioRepository.ObtenerPorIdAsync(id);

            if (beneficiario is null)
            {
                throw new RecursoNoEncontradoException($"No se encontro el beneficiario con Id {id}.");
            }

            // La cascada configurada en AppDbContext para AsignacionHerencia ->
            // Beneficiario es Restrict (no Cascade): si este beneficiario
            // todavia tiene asignaciones de herencia activas, la base de
            // datos va a RECHAZAR este DELETE (para no perder informacion de
            // "a quien se le asigno que"). Ese rechazo llega aca como una
            // excepcion tecnica generica, que el catch de abajo traduce a un
            // ReglaNegocioException con mensaje seguro.
            await _beneficiarioRepository.EliminarAsync(id);
        }
        catch (RecursoNoEncontradoException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ReglaNegocioException(
                "Ocurrio un error al eliminar el beneficiario. Verifique que no tenga asignaciones de herencia activas.", ex);
        }
    }

    // MapearADTO centraliza la conversion Entidad -> DTO de salida.
    private static BeneficiarioDTO MapearADTO(Beneficiario beneficiario)
    {
        return new BeneficiarioDTO
        {
            Id = beneficiario.Id,
            Nombre = beneficiario.Nombre,
            Email = beneficiario.Email,
            Parentesco = beneficiario.Parentesco,
            UsuarioId = beneficiario.UsuarioId
        };
    }
}
