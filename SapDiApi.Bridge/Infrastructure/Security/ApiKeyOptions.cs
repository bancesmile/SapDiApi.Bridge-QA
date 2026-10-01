namespace SapDiApi.Bridge.Infrastructure.Security
{
    public class ApiKeyOptions
    {
        /// <summary>
        /// Nombre del encabezado HTTP esperado (por defecto: X-Api-Key).
        /// </summary>
        public string HeaderName { get; set; } = ApiKeyConstants.DefaultHeaderName;

        /// <summary>
        /// Registro estructurado de aplicaciones/clientes con llaves, permisos y control de acceso por sociedad.
        /// </summary>
        public List<ApiClientConfig> Clients { get; set; } = new();

        /// <summary>
        /// Llave o llaves autorizadas heredadas (para compatibilidad hacia atrás).
        /// </summary>
        public List<string> ValidApiKeys { get; set; } = new();

        /// <summary>
        /// Clave maestra o secreta única (opcional / fallback).
        /// </summary>
        public string? SecretKey { get; set; }

        /// <summary>
        /// Obtiene el cliente configurado y activo que coincide con la llave provista.
        /// </summary>
        public ApiClientConfig? GetClient(string providedKey)
        {
            if (string.IsNullOrWhiteSpace(providedKey))
                return null;

            // 1. Buscar en el catálogo de clientes estructurados
            var client = Clients.FirstOrDefault(c => c.IsActive && string.Equals(c.ApiKey, providedKey, StringComparison.Ordinal));
            if (client != null)
                return client;

            // 2. Fallback: Llave secreta maestra legacy
            if (!string.IsNullOrWhiteSpace(SecretKey) && string.Equals(SecretKey, providedKey, StringComparison.Ordinal))
            {
                return new ApiClientConfig
                {
                    Id = "master-key",
                    Name = "Master Admin Key",
                    ApiKey = SecretKey,
                    IsActive = true,
                    AllowedCompanies = new List<string> { "*" }
                };
            }

            // 3. Fallback: Lista plana ValidApiKeys legacy
            if (ValidApiKeys.Any(k => string.Equals(k, providedKey, StringComparison.Ordinal)))
            {
                return new ApiClientConfig
                {
                    Id = "legacy-client",
                    Name = "Legacy API Client",
                    ApiKey = providedKey,
                    IsActive = true,
                    AllowedCompanies = new List<string> { "*" }
                };
            }

            return null;
        }

        /// <summary>
        /// Valida si una llave recibida coincide con algún cliente activo o llave legacy.
        /// </summary>
        public bool IsValidKey(string providedKey)
        {
            return GetClient(providedKey) != null;
        }

        /// <summary>
        /// Valida si el cliente tiene permisos para interactuar con la sociedad SAP especificada.
        /// Soporta validación por nombre de BD, código de empresa o ID numérico.
        /// </summary>
        public bool IsCompanyAllowed(ApiClientConfig? client, string companyDb, Models.Companies.CompanyDto? company = null)
        {
            if (client == null)
                return false;

            if (client.AllowedCompanies == null || !client.AllowedCompanies.Any())
                return true;

            if (client.AllowedCompanies.Any(c => c == "*"))
                return true;

            if (client.AllowedCompanies.Any(c => string.Equals(c, companyDb, StringComparison.OrdinalIgnoreCase)))
                return true;

            if (company != null)
            {
                if (client.AllowedCompanies.Any(c => string.Equals(c, company.CompanyId.ToString(), StringComparison.OrdinalIgnoreCase)))
                    return true;

                if (!string.IsNullOrWhiteSpace(company.CompanyCode) &&
                    client.AllowedCompanies.Any(c => string.Equals(c, company.CompanyCode, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Configuración de cliente/aplicación autorizada a consumir el Bridge.
    /// </summary>
    public class ApiClientConfig
    {
        /// <summary>
        /// Identificador único del cliente (ej: portal-autorizaciones).
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Nombre descriptivo del cliente (ej: Portal Web de Autorizaciones).
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Llave de API única asignada a este cliente.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Indica si el cliente está activo. Si es false, se rechazan las peticiones.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Lista de sociedades (CompanyDB) permitidas para este cliente. Usar ["*"] para permitir todas.
        /// </summary>
        public List<string> AllowedCompanies { get; set; } = new() { "*" };
    }
}
