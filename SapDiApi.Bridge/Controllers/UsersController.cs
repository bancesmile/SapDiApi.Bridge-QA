using Microsoft.AspNetCore.Mvc;
using SapDiApi.Bridge.Infrastructure.Security;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.Common;
using SapDiApi.Bridge.Models.Users;
using SapDiApi.Bridge.Services.Users;

namespace SapDiApi.Bridge.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [B1SessionAuth]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserService userService,
            ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Listar usuarios de SAP Business One
        /// </summary>
        /// <remarks>
        /// Obtiene la lista de usuarios (OUSR) con payload optimizado para administración.
        /// Filtros opcionales por texto (código o nombre), estado bloqueado, superusuario, sucursal o departamento.
        /// Compatible directamente con SAP Service Layer: `GET /b1s/v1/Users` o `GET /api/v1/Users`.
        /// </remarks>
        /// <response code="200">Lista de usuarios obtenida correctamente.</response>
        /// <response code="401">No autorizado - Sesión B1SESSION requerida.</response>
        [HttpGet]
        [Route("~/api/v1/Users")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAll([FromQuery] UserFilterDto filter)
        {
            var session = HttpContext.Items["UserSession"] as UserSession;
            var results = await _userService.GetFilteredAsync(filter, session);

            return Ok(ApiResponse<IEnumerable<UserDto>>.Ok(
                results,
                $"Usuarios recuperados para la sociedad '{session?.CompanyDB ?? "Default"}'"));
        }

        /// <summary>
        /// Consultar usuario por clave interna (InternalKey)
        /// </summary>
        /// <remarks>
        /// Obtiene el detalle administrativo completo de un usuario en SAP por su ID numérico interno (USERID).
        /// Incluye los campos visibles en la ventana de SAP (General, Servicios, Visualizar y UDFs).
        /// Compatible con formato Service Layer: `GET /b1s/v1/Users(200)` o `GET /api/v1/Users/200`.
        /// </remarks>
        /// <param name="id">Clave interna del usuario en SAP (InternalKey / USERID).</param>
        /// <param name="includePermissions">Si es true, incluye el listado de permisos del usuario (USR3).</param>
        /// <response code="200">Usuario encontrado.</response>
        /// <response code="404">No encontrado en SAP.</response>
        /// <response code="401">No autorizado - Sesión B1SESSION requerida.</response>
        [HttpGet]
        [Route("~/api/v1/Users({id:int})")]
        [Route("~/api/v1/Users/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetById(int id, [FromQuery] bool includePermissions = false)
        {
            var session = HttpContext.Items["UserSession"] as UserSession;
            var user = await _userService.GetByIdAsync(id, includePermissions, session);

            if (user == null)
            {
                return NotFound(ServiceLayerErrorResponse.Create(404, $"El usuario con InternalKey #{id} no existe en SAP."));
            }

            return Ok(ApiResponse<UserDto>.Ok(user, "Usuario recuperado exitosamente."));
        }

        /// <summary>
        /// Consultar usuario por código de usuario (UserCode)
        /// </summary>
        /// <remarks>
        /// Obtiene el detalle administrativo de un usuario por su código de inicio de sesión (ej: `Users('manager')` o `Users('creditos08')`).
        /// </remarks>
        /// <param name="userCode">Código de usuario en SAP (USER_CODE).</param>
        /// <param name="includePermissions">Si es true, incluye el listado de permisos del usuario (USR3).</param>
        /// <response code="200">Usuario encontrado.</response>
        /// <response code="404">No encontrado en SAP.</response>
        /// <response code="401">No autorizado - Sesión B1SESSION requerida.</response>
        [HttpGet]
        [Route("~/api/v1/Users('{userCode}')")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetByCode(string userCode, [FromQuery] bool includePermissions = false)
        {
            var cleanCode = userCode.Trim('\'', '\"');
            var session = HttpContext.Items["UserSession"] as UserSession;

            var user = await _userService.GetByCodeAsync(cleanCode, includePermissions, session);
            if (user == null)
            {
                return NotFound(ServiceLayerErrorResponse.Create(404, $"El usuario con código '{cleanCode}' no existe en SAP."));
            }

            return Ok(ApiResponse<UserDto>.Ok(user, "Usuario recuperado exitosamente."));
        }

        /// <summary>
        /// Crear usuario en SAP Business One
        /// </summary>
        /// <remarks>
        /// Crea un nuevo usuario en SAP Business One con sus credenciales iniciales, parámetros generales y campos UDF.
        /// Compatible directamente con formato de Service Layer: `POST /b1s/v1/Users` o `POST /api/v1/Users`.
        /// </remarks>
        /// <param name="dto">Datos del nuevo usuario.</param>
        /// <response code="201">Usuario creado exitosamente en SAP.</response>
        /// <response code="400">Error de validación o rechazo por SAP.</response>
        /// <response code="401">No autorizado - Sesión B1SESSION requerida.</response>
        [HttpPost]
        [Route("~/api/v1/Users")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserCode))
            {
                return BadRequest(ServiceLayerErrorResponse.Create(400, "El código de usuario (UserCode) es obligatorio."));
            }

            if (string.IsNullOrWhiteSpace(dto.UserPassword))
            {
                return BadRequest(ServiceLayerErrorResponse.Create(400, "La contraseña del usuario (UserPassword) es obligatoria al crear."));
            }

            var session = HttpContext.Items["UserSession"] as UserSession;
            var (success, internalKey, errorMessage) = await _userService.CreateAsync(dto, session);

            if (!success)
            {
                return BadRequest(ServiceLayerErrorResponse.Create(400, errorMessage ?? "Fallo al crear usuario en SAP."));
            }

            return CreatedAtAction(
                nameof(GetById),
                new { id = internalKey },
                ApiResponse<object>.Ok(new { InternalKey = internalKey, UserCode = dto.UserCode }, $"Usuario '{dto.UserCode}' creado exitosamente en SAP con InternalKey #{internalKey}."));
        }

        /// <summary>
        /// Actualizar usuario en SAP Business One
        /// </summary>
        /// <remarks>
        /// Actualiza un usuario existente por su clave interna (InternalKey).
        /// Permite modificar nombre, estado de bloqueo, superusuario, correo, teléfono, sucursal, departamento y campos de usuario (UDFs).
        /// Compatible con `PATCH / PUT /b1s/v1/Users(200)` o `/api/v1/Users(200)`.
        /// </remarks>
        /// <param name="id">Clave interna del usuario (InternalKey).</param>
        /// <param name="dto">Campos a actualizar.</param>
        /// <response code="200">Usuario actualizado exitosamente.</response>
        /// <response code="400">Error en la actualización de SAP.</response>
        /// <response code="401">No autorizado - Sesión B1SESSION requerida.</response>
        [HttpPatch]
        [HttpPut]
        [Route("~/api/v1/Users({id:int})")]
        [Route("~/api/v1/Users/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
        {
            var session = HttpContext.Items["UserSession"] as UserSession;
            var (success, internalKey, errorMessage) = await _userService.UpdateAsync(id, dto, session);

            if (!success)
            {
                return BadRequest(ServiceLayerErrorResponse.Create(400, errorMessage ?? $"Fallo al actualizar el usuario #{id} en SAP."));
            }

            return Ok(ApiResponse<object>.Ok(
                new { InternalKey = internalKey },
                $"Usuario #{internalKey} actualizado exitosamente en SAP."));
        }

        /// <summary>
        /// Cambiar o restablecer contraseña de un usuario
        /// </summary>
        /// <remarks>
        /// Permite a un administrador cambiar o resetear la clave de acceso de un usuario en SAP Business One.
        /// Opcionalmente permite forzar cambio de clave en el próximo inicio de sesión (`ChangePasswordNextLogon: "tYES"`)
        /// y/o configurar si nunca vence (`PasswordNeverExpires: "tYES"`).
        /// Endpoint: `POST /api/v1/Users(200)/ChangePassword` o `/b1s/v1/Users(200)/ChangePassword`.
        /// </remarks>
        /// <param name="id">Clave interna del usuario (InternalKey).</param>
        /// <param name="dto">Nueva contraseña y banderas opcionales de expiración/próximo inicio de sesión.</param>
        /// <response code="200">Contraseña cambiada exitosamente.</response>
        /// <response code="400">Error al cambiar contraseña.</response>
        /// <response code="401">No autorizado - Sesión B1SESSION requerida.</response>
        [HttpPost]
        [Route("~/api/v1/Users({id:int})/ChangePassword")]
        [Route("~/api/v1/Users/{id:int}/ChangePassword")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangeUserPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                return BadRequest(ServiceLayerErrorResponse.Create(400, "La nueva contraseña (NewPassword) no puede estar vacía."));
            }

            var session = HttpContext.Items["UserSession"] as UserSession;
            var (success, internalKey, errorMessage) = await _userService.ChangePasswordAsync(id, dto, session);

            if (!success)
            {
                return BadRequest(ServiceLayerErrorResponse.Create(400, errorMessage ?? $"Fallo al cambiar la contraseña del usuario #{id}."));
            }

            return Ok(ApiResponse<object>.Ok(
                new { InternalKey = internalKey },
                $"Contraseña del usuario #{internalKey} cambiada exitosamente en SAP."));
        }

        /// <summary>
        /// Actualizar usuario por código de usuario (UserCode)
        /// </summary>
        /// <remarks>
        /// Ideal para operaciones multi-empresa donde el UserCode es idéntico pero el InternalKey varía entre bases de datos.
        /// Endpoint: `PATCH /api/v1/Users('creditos08')` o `/b1s/v1/Users('creditos08')`.
        /// </remarks>
        [HttpPatch]
        [HttpPut]
        [Route("~/api/v1/Users('{userCode}')")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateByCode(string userCode, [FromBody] UpdateUserDto dto)
        {
            var cleanCode = userCode.Trim('\'', '\"');
            var session = HttpContext.Items["UserSession"] as UserSession;
            var (success, internalKey, errorMessage) = await _userService.UpdateByCodeAsync(cleanCode, dto, session);

            if (!success)
            {
                return BadRequest(ServiceLayerErrorResponse.Create(400, errorMessage ?? $"Fallo al actualizar el usuario '{cleanCode}' en SAP."));
            }

            return Ok(ApiResponse<object>.Ok(
                new { UserCode = cleanCode, InternalKey = internalKey },
                $"Usuario '{cleanCode}' actualizado exitosamente en SAP."));
        }

        /// <summary>
        /// Cambiar o restablecer contraseña por código de usuario (UserCode)
        /// </summary>
        /// <remarks>
        /// Ideal para reset masivo o multi-empresa. Permite además forzar cambio de contraseña en próxima conexión
        /// enviando `ChangePasswordNextLogon: "tYES"`.
        /// Endpoint: `POST /api/v1/Users('creditos08')/ChangePassword` o `/b1s/v1/Users('creditos08')/ChangePassword`.
        /// </remarks>
        [HttpPost]
        [Route("~/api/v1/Users('{userCode}')/ChangePassword")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePasswordByCode(string userCode, [FromBody] ChangeUserPasswordDto dto)
        {
            var cleanCode = userCode.Trim('\'', '\"');
            if (string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                return BadRequest(ServiceLayerErrorResponse.Create(400, "La nueva contraseña (NewPassword) no puede estar vacía."));
            }

            var session = HttpContext.Items["UserSession"] as UserSession;
            var (success, internalKey, errorMessage) = await _userService.ChangePasswordByCodeAsync(cleanCode, dto, session);

            if (!success)
            {
                return BadRequest(ServiceLayerErrorResponse.Create(400, errorMessage ?? $"Fallo al cambiar la contraseña del usuario '{cleanCode}'."));
            }

            return Ok(ApiResponse<object>.Ok(
                new { UserCode = cleanCode, InternalKey = internalKey },
                $"Contraseña del usuario '{cleanCode}' cambiada exitosamente en SAP."));
        }
    }
}
