namespace SapDiApi.Bridge.Infrastructure.Security
{
    public class ApiKeyOptions
    {
        /// <summary>
        /// Nombre del encabezado HTTP esperado (por defecto: X-Api-Key).
        /// </summary>
        public string HeaderName { get; set; } = ApiKeyConstants.DefaultHeaderName;

        /// <summary>
        /// Llave o llaves autorizadas para consumir la API.
        /// </summary>
        public List<string> ValidApiKeys { get; set; } = new();

        /// <summary>
        /// Clave maestra o secreta única (opcional / fallback).
        /// </summary>
        public string? SecretKey { get; set; }

        /// <summary>
        /// Valida si una llave recibida coincide con alguna configurada.
        /// </summary>
        public bool IsValidKey(string providedKey)
        {
            if (string.IsNullOrWhiteSpace(providedKey))
                return false;

            if (!string.IsNullOrWhiteSpace(SecretKey) && string.Equals(SecretKey, providedKey, StringComparison.Ordinal))
                return true;

            return ValidApiKeys.Any(k => string.Equals(k, providedKey, StringComparison.Ordinal));
        }
    }
}
