using System.Security.Claims;
using Herencia.Business.Dtos;
using Herencia.Business.Exceptions;
using Herencia.Business.Interfaces;
using Herencia.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Herencia.Api.Controllers;

// UsuariosController es la puerta de entrada HTTP para todo lo relacionado a
// Usuario. Es la capa mas EXTERNA de la arquitectura: su unica responsabilidad
// es (1) recibir la request HTTP, (2) delegar TODA la logica al servicio de
// Business correspondiente, y (3) traducir el resultado (o la excepcion) a un
// codigo de estado HTTP y un cuerpo de respuesta.
//
// [ApiController] habilita, entre otras cosas, la VALIDACION AUTOMATICA del
// ModelState: si el DTO de entrada (ej: UsuarioCreacionDTO) no cumple sus
// Data Annotations ([Required], etc.), ASP.NET Core devuelve un 400 Bad Request
// el mismo, ANTES de que el codigo del metodo del action llegue a ejecutarse.
//
// [Route("api/usuarios")] fija el prefijo de ruta base RESTful para todo el
// recurso "usuarios", en linea con la convencion "api/{recurso-en-plural}".
//
// --- [Authorize] a nivel de clase ---
// Los datos de un Usuario (Nombre, Email) son informacion personal: ningun
// endpoint deberia quedar accesible sin un Token JWT valido. Igual que en
// ActivosDigitalesController, se protege TODO el controller por defecto
// ("secure by default"), y se marca explicitamente con [AllowAnonymous] el
// UNICO endpoint que necesita quedar publico (Crear), en vez de arriesgarse a
// que un endpoint nuevo quede sin proteger por simple omision.
[ApiController]
[Authorize]
[Route("api/usuarios")]
public class UsuariosController : ControllerBase
{
    // --- Inyeccion de Dependencias por CONSTRUCTOR ---
    // El controller depende UNICAMENTE de las INTERFACES de la capa Business
    // (IUsuarioService, IActivoDigitalService), jamas de IUsuarioRepository,
    // IActivoDigitalRepository ni de AppDbContext. Esto es lo que exige la
    // rubrica de Arquitectura Limpia: el controller no sabe (ni le importa)
    // que hay una base de datos SQLite detras, ni como estan armadas las
    // consultas EF Core; solo conoce "que operaciones de negocio existen".
    // Esto mantiene la base de datos completamente AISLADA detras de dos
    // capas (Business y Data), invisible para la capa de presentacion.
    private readonly IUsuarioService _usuarioService;

    // Se inyecta tambien IActivoDigitalService (y no IActivoDigitalRepository)
    // porque este controller expone la ruta anidada "GET api/usuarios/{id}/activos"
    // (los activos digitales de un usuario puntual). Al depender de la interfaz
    // de servicio, seguimos respetando la regla de "nunca repositorios en el
    // controller", aun cuando el recurso pertenece a otro controller (ActivosDigitalesController).
    private readonly IActivoDigitalService _activoDigitalService;

    // ILogger permite dejar constancia, del lado del SERVIDOR, del detalle
    // tecnico real de una excepcion inesperada (para diagnostico interno),
    // sin que ese detalle llegue nunca al cliente de la Api (ver el catch
    // generico de cada endpoint mas abajo).
    private readonly ILogger<UsuariosController> _logger;

    public UsuariosController(
        IUsuarioService usuarioService,
        IActivoDigitalService activoDigitalService,
        ILogger<UsuariosController> logger)
    {
        _usuarioService = usuarioService;
        _activoDigitalService = activoDigitalService;
        _logger = logger;
    }

    // --- Helper privado: extraer el Id del usuario autenticado del Token JWT ---
    // Mismo criterio que en ActivosDigitalesController: centraliza la lectura
    // del Claim ClaimTypes.NameIdentifier para no repetirla en cada action.
    private int? ObtenerUsuarioIdAutenticado()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return (claim is not null && int.TryParse(claim.Value, out var usuarioId)) ? usuarioId : null;
    }

    // GET api/usuarios
    //
    // Verbo GET: por definicion del protocolo HTTP, GET es una operacion de
    // SOLO LECTURA, segura e idempotente (llamarla una o mil veces produce el
    // mismo resultado y no modifica ningun estado del servidor). Por eso es el
    // verbo correcto para "obtener el listado completo de usuarios".
    //
    // --- [Authorize(Roles = "Administrador")] ---
    // A diferencia del resto de los endpoints de este controller (protegidos
    // por el [Authorize] de clase, que solo exige "estar logueado"), listar
    // TODOS los usuarios del sistema es una operacion administrativa: ningun
    // usuario "comun" (RolUsuario.Usuario) tiene motivo legitimo para ver los
    // datos de TODOS los demas usuarios, solo los suyos propios (ver
    // ObtenerPorId). [Authorize(Roles = "Administrador")] agrega una
    // capa MAS de autorizacion encima de la autenticacion: no alcanza con
    // tener un token valido, ese token ademas debe traer el Claim de rol
    // "Administrador" (ver TokenService.CrearToken). Si un usuario
    // autenticado pero con rol "Usuario" intenta este endpoint, el middleware
    // de Authorization lo corta con un 403 Forbidden automatico, sin llegar a
    // ejecutar el codigo de este metodo.
    [Authorize(Roles = nameof(RolUsuario.Administrador))]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioDTO>>> ObtenerTodos()
    {
        try
        {
            var usuarios = await _usuarioService.ObtenerTodosLosUsuariosAsync();

            // 200 OK: el codigo de exito estandar para un GET que se resolvio
            // correctamente, independientemente de si la lista viene vacia o
            // con elementos (una lista vacia SIGUE siendo una respuesta valida,
            // no un error).
            return Ok(usuarios);
        }
        catch (Exception ex)
        {
            // Cualquier error no esperado (ej: la base de datos no responde)
            // se traduce a 500 Internal Server Error. Nunca devolvemos el
            // mensaje de "ex" ni su StackTrace al cliente: eso podria filtrar
            // detalles de infraestructura. El detalle tecnico completo queda
            // registrado solo en el log del servidor, para diagnostico interno.
            _logger.LogError(ex, "Error inesperado al obtener el listado de usuarios.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    // GET api/usuarios/{id}
    //
    // Verbo GET sobre una ruta con el Id como parametro: es la forma RESTful
    // estandar de pedir UN recurso puntual (no una coleccion).
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsuarioDTO>> ObtenerPorId(int id)
    {
        try
        {
            // --- Verificacion de OWNERSHIP (IDOR) ---
            // Sin este chequeo, cualquier usuario autenticado podria leer el
            // perfil (Nombre, Email) de CUALQUIER OTRO usuario con solo
            // adivinar/enumerar Ids en la URL. Como el sistema no maneja
            // roles de administrador, la unica regla posible hoy es "un
            // usuario solo puede ver su PROPIO perfil". El Id de la URL se
            // compara contra el Id extraido del Token JWT (no falsificable
            // sin invalidar la firma, ver TokenService).
            var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

            if (usuarioAutenticadoId is null)
            {
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            if (id != usuarioAutenticadoId)
            {
                // 403 Forbidden: token valido, pero sin permiso sobre ESTE
                // perfil puntual (no es el propio).
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para acceder a este usuario." });
            }

            var usuario = await _usuarioService.ObtenerUsuarioPorIdAsync(id);

            // 200 OK: se encontro el usuario solicitado y es el propio.
            return Ok(usuario);
        }
        catch (RecursoNoEncontradoException ex)
        {
            // El servicio de Business detecto que el Id no corresponde a
            // ningun Usuario existente. Semanticamente, esto es exactamente
            // lo que representa un 404 Not Found: "el recurso solicitado no
            // existe". Se devuelve el Message de la excepcion porque, tal
            // como esta documentado en RecursoNoEncontradoException, ese
            // mensaje fue pensado deliberadamente para ser "amigable" y
            // seguro de mostrar al cliente (no expone detalles tecnicos).
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al obtener el usuario con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    // GET api/usuarios/{id}/activos
    //
    // Ruta semantica ANIDADA: expresa la relacion de dominio "un Usuario TIENE
    // muchos ActivosDigitales" directamente en la URL, sin necesidad de un
    // query string (ej: no usamos "api/activosdigitales?usuarioId=5"). Sigue
    // siendo un GET porque es una consulta de solo lectura.
    [HttpGet("{id:int}/activos")]
    public async Task<ActionResult<IEnumerable<ActivoDigitalDTO>>> ObtenerActivosDelUsuario(int id)
    {
        try
        {
            // Misma verificacion de OWNERSHIP que en ObtenerPorId: los
            // ActivosDigitales son informacion sensible, asi que un usuario
            // solo puede listar los SUYOS (id de la URL == id del token). El
            // endpoint hermano "GET /api/activos" (ActivosDigitalesController)
            // ya resuelve este mismo caso de uso sin necesitar recibir ningun
            // Id en la URL; esta ruta anidada se mantiene por compatibilidad
            // con lo ya implementado, pero ahora protegida de la misma forma.
            var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

            if (usuarioAutenticadoId is null)
            {
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            if (id != usuarioAutenticadoId)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para acceder a los activos de este usuario." });
            }

            // Notar que el controller delega ESTA logica (incluida la
            // validacion de que el usuario exista) al servicio de
            // ActivoDigital, no la reimplementa aca. El controller no sabe
            // como se resuelve la consulta, solo que existe este metodo.
            var activos = await _activoDigitalService.ObtenerActivosPorUsuarioAsync(id);

            return Ok(activos);
        }
        catch (RecursoNoEncontradoException ex)
        {
            // El usuario titular indicado en la ruta no existe: 404 Not Found.
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al obtener los activos del usuario con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    // POST api/usuarios
    //
    // Verbo POST: se usa para CREAR un nuevo recurso cuyo identificador todavia
    // no existe (el cliente no elige el Id, lo genera la base de datos). A
    // diferencia de GET/PUT/DELETE, POST no es idempotente: llamarlo dos veces
    // con el mismo body crea DOS usuarios distintos.
    //
    // --- ¿Por que [AllowAnonymous] aca, si el controller entero es [Authorize]? ---
    // Este es, deliberadamente, el UNICO endpoint publico de todo el
    // controller: no tendria sentido exigir estar ya autenticado para poder
    // crear una cuenta nueva (seria una paradoja: necesitarias un token para
    // conseguir tu primer token). El flujo de auto-registro "oficial" para un
    // visitante anonimo es "POST /api/auth/registro" (AuthController), que
    // reutiliza este mismo servicio; este endpoint se mantiene [AllowAnonymous]
    // por compatibilidad con el alta administrativa de Usuarios ya implementada
    // antes de que existiera el modulo de autenticacion.
    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult<UsuarioDTO>> Crear(UsuarioCreacionDTO usuarioCreacionDTO)
    {
        // Chequeo defensivo explicito del ModelState. Con [ApiController] este
        // caso ya se resuelve automaticamente con un 400 antes de llegar aca
        // (por los [Required] del DTO), pero se deja el chequeo a la vista
        // para que quede documentado en el propio codigo del endpoint el
        // motivo del 400 en caso de datos estructuralmente invalidos.
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var usuarioCreado = await _usuarioService.CrearUsuarioAsync(usuarioCreacionDTO);

            // 201 Created: es el codigo HTTP correcto para un POST que creo
            // exitosamente un recurso nuevo. CreatedAtAction, ademas de fijar
            // el status code, arma automaticamente el header "Location" de la
            // respuesta apuntando a la URL donde se puede CONSULTAR el recurso
            // recien creado (en este caso, "GET api/usuarios/{id}" a traves
            // de la action ObtenerPorId), tal como exige el estandar REST.
            return CreatedAtAction(
                nameof(ObtenerPorId),
                new { id = usuarioCreado.Id },
                usuarioCreado);
        }
        catch (ReglaNegocioException ex)
        {
            // El servicio detecto que los datos de entrada violan una regla de
            // negocio (nombre vacio, email con formato invalido, contrasena
            // demasiado corta, etc.). Esto se traduce a 400 Bad Request: el
            // problema es responsabilidad del CLIENTE (envio datos invalidos),
            // no del servidor.
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al crear un usuario.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    // PUT api/usuarios/{id}
    //
    // Verbo PUT: se usa para ACTUALIZAR un recurso EXISTENTE e identificado
    // por su Id (que viaja en la URL, no en el body). A diferencia de POST,
    // PUT SI es idempotente: enviar la misma request varias veces deja al
    // usuario en el mismo estado final, sin crear registros duplicados.
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UsuarioDTO>> Actualizar(int id, UsuarioActualizacionDTO usuarioActualizacionDTO)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // --- Verificacion de OWNERSHIP: un usuario solo puede editar su
            // PROPIO perfil, nunca el de otro (aunque adivine su Id). ---
            var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

            if (usuarioAutenticadoId is null)
            {
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            if (id != usuarioAutenticadoId)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para modificar este usuario." });
            }

            var usuarioActualizado = await _usuarioService.ActualizarUsuarioAsync(id, usuarioActualizacionDTO);

            // 200 OK: la actualizacion se aplico correctamente. Se devuelve el
            // recurso ya actualizado en el body para que el cliente pueda
            // confirmar el nuevo estado sin tener que pedirlo con un GET aparte.
            return Ok(usuarioActualizado);
        }
        catch (RecursoNoEncontradoException ex)
        {
            // El Id de la URL no corresponde a ningun usuario existente: no
            // hay nada que actualizar. 404 Not Found.
            return NotFound(new { mensaje = ex.Message });
        }
        catch (ReglaNegocioException ex)
        {
            // Los nuevos datos (Nombre/Email) violan una regla de negocio:
            // 400 Bad Request, responsabilidad del cliente.
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al actualizar el usuario con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    // PUT api/usuarios/{id}/password
    //
    // Verbo PUT: reemplaza por completo la credencial de contraseña del
    // Usuario (no es una modificacion parcial de un recurso con varios
    // campos independientes, como si lo era PATCH .../estado en
    // AsignacionesController: aca hay un unico "secreto" que se reemplaza
    // entero). Requiere conocer la contraseña ACTUAL (ver CambiarPasswordDTO).
    [HttpPut("{id:int}/password")]
    public async Task<IActionResult> CambiarPassword(int id, CambiarPasswordDTO cambiarPasswordDTO)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // --- Verificacion de OWNERSHIP: un usuario solo puede cambiar
            // su PROPIA contraseña, nunca la de otro (aunque adivine su Id). ---
            var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

            if (usuarioAutenticadoId is null)
            {
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            if (id != usuarioAutenticadoId)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para cambiar la contraseña de este usuario." });
            }

            await _usuarioService.CambiarPasswordAsync(id, cambiarPasswordDTO);

            // 204 No Content: la contraseña se cambio con exito. No hay
            // ningun dato sensible que devolver en el body (jamas se
            // devuelve un hash ni la contraseña en texto plano).
            return NoContent();
        }
        catch (RecursoNoEncontradoException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (ReglaNegocioException ex)
        {
            // La contraseña actual ingresada no coincide, o la nueva no
            // cumple el largo minimo: 400 Bad Request, responsabilidad del
            // cliente.
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al cambiar la contraseña del usuario con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }

    // DELETE api/usuarios/{id}
    //
    // Verbo DELETE: se usa para ELIMINAR el recurso identificado por el Id de
    // la URL. Al igual que PUT, es idempotente: borrar dos veces el mismo Id
    // deja el mismo estado final (el usuario no existe), aunque la segunda
    // llamada ya no encuentre nada para borrar (de ahi el 404 en ese caso).
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        try
        {
            // --- Verificacion de OWNERSHIP: un usuario solo puede eliminar
            // su PROPIA cuenta, nunca la de otro. ---
            var usuarioAutenticadoId = ObtenerUsuarioIdAutenticado();

            if (usuarioAutenticadoId is null)
            {
                return Unauthorized(new { mensaje = "El token no contiene un identificador de usuario valido." });
            }

            if (id != usuarioAutenticadoId)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { mensaje = "No tenes permiso para eliminar este usuario." });
            }

            await _usuarioService.EliminarUsuarioAsync(id);

            // 204 No Content: el borrado se realizo con exito. No se devuelve
            // ningun cuerpo en la respuesta porque, al haberse eliminado el
            // recurso, ya no hay nada que representar; devolver un body vacio
            // (en vez de, por ejemplo, un 200 con el usuario borrado) es la
            // convencion REST estandar para operaciones DELETE exitosas.
            return NoContent();
        }
        catch (RecursoNoEncontradoException ex)
        {
            // El Id no corresponde a ningun usuario existente: no hay nada
            // que borrar. 404 Not Found.
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al eliminar el usuario con Id {Id}.", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { mensaje = "Ocurrio un error interno al procesar la solicitud." });
        }
    }
}
