    public class CE_SAPConnection
    {
        public string Server { get; set; } = ConfigurationManager.AppSettings["SAP_Server"];
        public string LicenseServer { get; set; } = ConfigurationManager.AppSettings["SAP_LicenseServer"];
        public string CompanyDB { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool Connected { get; set; }
        public string ErrorMessage { get; set; }
        public int ErrorCode { get; set; }
        public DateTime LastConnectionAttempt { get; set; }
        public int ConnectionAttempts { get; set; }

        public CE_SAPConnection() 
        {
            ConnectionAttempts = 0;
            LastConnectionAttempt = DateTime.MinValue;
        }

        public CE_SAPConnection(string companyDB, string userName, string password)
        {
            CompanyDB = companyDB;
            UserName = userName;
            Password = password;
            ConnectionAttempts = 0;
            LastConnectionAttempt = DateTime.MinValue;
        }

        /// <summary>
        /// Registra un intento de conexión
        /// </summary>
        public void RegistrarIntentoConexion(int errorCode, string errorMessage)
        {
            ConnectionAttempts++;
            LastConnectionAttempt = DateTime.Now;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Reinicia los contadores de conexión
        /// </summary>
        public void ReiniciartContadores()
        {
            ConnectionAttempts = 0;
            ErrorCode = 0;
            ErrorMessage = null;
            Connected = false;
        }

        /// <summary>
        /// Verifica si la conexión ha tenido demasiados intentos fallidos
        /// </summary>
        public bool DemasiadosIntentosFallidos(int maxIntentos = 5)
        {
            return ConnectionAttempts >= maxIntentos;
        }

        /// <summary>
        /// Genera una clave única para esta conexión
        /// </summary>
        public string GenerarClaveConexion()
        {
            return $"{CompanyDB}_{UserName}_{Server}".ToLower();
        }

        /// <summary>
        /// Verifica si es un error conocido que requiere reintento
        /// </summary>
        public bool EsErrorRecuperable(int errorCode)
        {
            switch (errorCode)
            {
                case 100000085: // Add-on has already logged on with the same account
                case -5002:     // Internal error
                case -1116:     // Unknown error
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Calcula el tiempo de espera recomendado antes del siguiente intento
        /// </summary>
        public int CalcularTiempoEspera()
        {
            // Espera progresiva: 1s, 2s, 3s, 5s, 8s
            switch (ConnectionAttempts)
            {
                case 1: return 1000;
                case 2: return 2000;
                case 3: return 3000;
                case 4: return 5000;
                default: return 8000;
            }
        }

        public override string ToString()
        {
            return $"SAP Connection: {CompanyDB}@{Server} (User: {UserName}, Connected: {Connected}, Attempts: {ConnectionAttempts})";
        }
    }
}