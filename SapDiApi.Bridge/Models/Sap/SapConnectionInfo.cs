namespace SapDiApi.Bridge.Models.Sap
{
    /// <summary>
    /// Modelo de conexión a SAP DI API con control de intentos, errores recuperables y backoff progresivo.
    /// </summary>
    public class SapConnectionInfo
    {
        public string Server { get; set; } = string.Empty;
        public string LicenseServer { get; set; } = string.Empty;
        public string? SLDServer { get; set; }
        public string CompanyDB { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string DbServerType { get; set; } = "dst_HANADB";
        public string? DbUserName { get; set; }
        public string? DbPassword { get; set; }
        public bool UseTrusted { get; set; } = false;

        public bool Connected { get; set; }
        public string? ErrorMessage { get; set; }
        public int ErrorCode { get; set; }
        public DateTime LastConnectionAttempt { get; set; } = DateTime.MinValue;
        public int ConnectionAttempts { get; set; }

        public SapConnectionInfo()
        {
            ConnectionAttempts = 0;
            LastConnectionAttempt = DateTime.MinValue;
        }

        public SapConnectionInfo(string companyDb, string userName, string password)
        {
            CompanyDB = companyDb;
            UserName = userName;
            Password = password;
            ConnectionAttempts = 0;
            LastConnectionAttempt = DateTime.MinValue;
        }

        /// <summary>
        /// Registra un intento de conexión fallido
        /// </summary>
        public void RegistrarIntentoConexion(int errorCode, string errorMessage)
        {
            ConnectionAttempts++;
            LastConnectionAttempt = DateTime.UtcNow;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            Connected = false;
        }

        /// <summary>
        /// Reinicia los contadores de conexión tras un éxito
        /// </summary>
        public void ReiniciarContadores()
        {
            ConnectionAttempts = 0;
            ErrorCode = 0;
            ErrorMessage = null;
            Connected = true;
        }

        /// <summary>
        /// Verifica si la conexión ha superado el umbral de intentos fallidos
        /// </summary>
        public bool DemasiadosIntentosFallidos(int maxIntentos = 5)
        {
            return ConnectionAttempts >= maxIntentos;
        }

        /// <summary>
        /// Genera una clave única de conexión para pooling o caché
        /// </summary>
        public string GenerarClaveConexion()
        {
            return $"{CompanyDB}_{UserName}_{Server}".ToLowerInvariant();
        }

        /// <summary>
        /// Evalúa si el código de error devuelto por SAP DI API es recuperable mediante reintento
        /// </summary>
        public bool EsErrorRecuperable(int errorCode)
        {
            return errorCode switch
            {
                100000085 => true, // Add-on has already logged on with the same account
                -5002     => true, // Internal error
                -1116     => true, // Unknown / temporary network error
                -107      => true, // Connection timed out
                _         => false
            };
        }

        /// <summary>
        /// Calcula el tiempo de espera progresivo antes del siguiente reintento (en ms)
        /// </summary>
        public int CalcularTiempoEsperaMs()
        {
            return ConnectionAttempts switch
            {
                1 => 1000,
                2 => 2000,
                3 => 3000,
                4 => 5000,
                _ => 8000
            };
        }

        public override string ToString()
        {
            return $"SAP Connection: {CompanyDB}@{Server} (User: {UserName}, Connected: {Connected}, Attempts: {ConnectionAttempts})";
        }
    }
}
