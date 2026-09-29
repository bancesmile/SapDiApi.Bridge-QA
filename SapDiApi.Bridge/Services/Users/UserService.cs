using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.Users;
using SapDiApi.Bridge.Services.Sap;

namespace SapDiApi.Bridge.Services.Users
{
    public class UserService : IUserService
    {
        private readonly ISapDiApiConnector _sapConnector;
        private readonly ILogger<UserService> _logger;

        public UserService(ISapDiApiConnector sapConnector, ILogger<UserService> logger)
        {
            _sapConnector = sapConnector;
            _logger = logger;
        }

        public IQueryable<UserDto> GetUsersQueryable()
        {
            return new List<UserDto>().AsQueryable();
        }

        public async Task<UserDto?> GetByIdAsync(int internalKey, bool includePermissions = false, UserSession? session = null)
        {
            if (session == null)
            {
                _logger.LogWarning("Intento de consulta a Usuario #{InternalKey} sin sesión activa.", internalKey);
                return null;
            }

            _logger.LogInformation("Consultando Usuario #{InternalKey} en SAP DI API | Usuario: {User} | DB: {DB}",
                internalKey, session.UserName, session.CompanyDB);

            return await _sapConnector.GetUserByIdAsync(session, internalKey, includePermissions);
        }

        public async Task<UserDto?> GetByCodeAsync(string userCode, bool includePermissions = false, UserSession? session = null)
        {
            if (session == null)
            {
                _logger.LogWarning("Intento de consulta a Usuario '{UserCode}' sin sesión activa.", userCode);
                return null;
            }

            _logger.LogInformation("Consultando Usuario '{UserCode}' en SAP DI API | Usuario: {User} | DB: {DB}",
                userCode, session.UserName, session.CompanyDB);

            return await _sapConnector.GetUserByCodeAsync(session, userCode, includePermissions);
        }

        public async Task<IEnumerable<UserDto>> GetFilteredAsync(UserFilterDto filter, UserSession? session = null)
        {
            if (session == null)
            {
                _logger.LogWarning("Intento de listar Usuarios sin sesión activa.");
                return Enumerable.Empty<UserDto>();
            }

            return await _sapConnector.GetUsersFilteredAsync(session, filter);
        }

        public async Task<(bool Success, int InternalKey, string? ErrorMessage)> CreateAsync(CreateUserDto dto, UserSession? session = null)
        {
            if (session == null)
            {
                return (false, 0, "Se requiere una sesión activa (B1SESSION) para crear usuarios en SAP.");
            }

            _logger.LogInformation("Creando nuevo Usuario '{UserCode}' ({UserName}) en SAP | Operador: {AuditUser} | DB: {DB}",
                dto.UserCode, dto.UserName, session.AuditUser ?? session.UserName, session.CompanyDB);

            return await _sapConnector.CreateUserAsync(session, dto);
        }

        public async Task<(bool Success, int InternalKey, string? ErrorMessage)> UpdateAsync(int internalKey, UpdateUserDto dto, UserSession? session = null)
        {
            if (session == null)
            {
                return (false, internalKey, "Se requiere una sesión activa (B1SESSION) para actualizar usuarios en SAP.");
            }

            _logger.LogInformation("Actualizando Usuario #{InternalKey} en SAP | Operador: {AuditUser} | DB: {DB}",
                internalKey, session.AuditUser ?? session.UserName, session.CompanyDB);

            return await _sapConnector.UpdateUserAsync(session, internalKey, dto);
        }

        public async Task<(bool Success, int InternalKey, string? ErrorMessage)> ChangePasswordAsync(int internalKey, ChangeUserPasswordDto dto, UserSession? session = null)
        {
            if (session == null)
            {
                return (false, internalKey, "Se requiere una sesión activa (B1SESSION) para cambiar contraseñas de usuarios en SAP.");
            }

            _logger.LogInformation("Cambiando contraseña de Usuario #{InternalKey} en SAP | Operador: {AuditUser} | DB: {DB}",
                internalKey, session.AuditUser ?? session.UserName, session.CompanyDB);

            return await _sapConnector.ChangeUserPasswordAsync(session, internalKey, dto);
        }

        public async Task<(bool Success, int InternalKey, string? ErrorMessage)> UpdateByCodeAsync(string userCode, UpdateUserDto dto, UserSession? session = null)
        {
            if (session == null)
            {
                return (false, 0, "Se requiere una sesión activa (B1SESSION) para actualizar usuarios en SAP.");
            }

            _logger.LogInformation("Actualizando Usuario '{UserCode}' en SAP | Operador: {AuditUser} | DB: {DB}",
                userCode, session.AuditUser ?? session.UserName, session.CompanyDB);

            return await _sapConnector.UpdateUserByCodeAsync(session, userCode, dto);
        }

        public async Task<(bool Success, int InternalKey, string? ErrorMessage)> ChangePasswordByCodeAsync(string userCode, ChangeUserPasswordDto dto, UserSession? session = null)
        {
            if (session == null)
            {
                return (false, 0, "Se requiere una sesión activa (B1SESSION) para cambiar contraseñas de usuarios en SAP.");
            }

            _logger.LogInformation("Cambiando contraseña de Usuario '{UserCode}' en SAP | Operador: {AuditUser} | DB: {DB}",
                userCode, session.AuditUser ?? session.UserName, session.CompanyDB);

            return await _sapConnector.ChangeUserPasswordByCodeAsync(session, userCode, dto);
        }
    }
}
