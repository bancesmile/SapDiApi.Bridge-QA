using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Services.Sap;

namespace SapDiApi.Bridge.Services.Auth
{
    public interface ISapAuthService
    {
        Task<(bool Success, string? ErrorMessage)> ValidateCredentialsAsync(LoginRequestDto request);
    }

    public class SapAuthService : ISapAuthService
    {
        private readonly ISapDiApiConnector _sapConnector;
        private readonly ILogger<SapAuthService> _logger;

        public SapAuthService(ISapDiApiConnector sapConnector, ILogger<SapAuthService> logger)
        {
            _sapConnector = sapConnector;
            _logger = logger;
        }

        public Task<(bool Success, string? ErrorMessage)> ValidateCredentialsAsync(LoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.CompanyDB) ||
                string.IsNullOrWhiteSpace(request.UserName) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return Task.FromResult<(bool Success, string? ErrorMessage)>((false, "Parámetros de conexión incompletos. Se requiere CompanyDB, UserName y Password."));
            }

            var session = new UserSession
            {
                CompanyDB = request.CompanyDB,
                UserName = request.UserName
            };

            // Validación directa contra SAP Business One DI API
            var (connected, errorMessage) = _sapConnector.Connect(session, request.Password);

            if (connected)
            {
                _logger.LogInformation("Autenticación SAP exitosa para usuario: {User} en BD: {DB}", request.UserName, request.CompanyDB);
                return Task.FromResult<(bool Success, string? ErrorMessage)>((true, null));
            }

            _logger.LogWarning("Fallo de autenticación en SAP DI API para usuario {User} en BD {DB}: {Error}",
                request.UserName, request.CompanyDB, errorMessage);

            return Task.FromResult<(bool Success, string? ErrorMessage)>((false, errorMessage ?? "Credenciales de SAP incorrectas o servidor no disponible."));
        }
    }
}
