using System.ComponentModel.DataAnnotations;

namespace SapDiApi.Bridge.Models.Auth
{
    /// <summary>
    /// Credenciales para iniciar sesión estilo SAP Service Layer.
    /// </summary>
    public class LoginRequestDto
    {
        /// <summary>
        /// Base de datos / Sociedad de SAP Business One.
        /// </summary>
        [Required(ErrorMessage = "La base de datos (CompanyDB) es obligatoria.")]
        public string CompanyDB { get; set; } = string.Empty;

        /// <summary>
        /// Usuario de SAP Business One.
        /// </summary>
        [Required(ErrorMessage = "El nombre de usuario (UserName) es obligatorio.")]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Contraseña del usuario de SAP Business One.
        /// </summary>
        [Required(ErrorMessage = "La contraseña (Password) es obligatoria.")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Usuario u operador real en sistemas satélites sin licencia propia (opcional para auditoría).
        /// </summary>
        public string? AuditUser { get; set; }

        /// <summary>
        /// Nombre del sistema satélite consumidor (opcional para auditoría).
        /// </summary>
        public string? AuditApp { get; set; }

        /// <summary>
        /// Idioma opcional (por defecto 23 = Español Latinoamérica).
        /// </summary>
        public int? Language { get; set; } = 23;
    }

    /// <summary>
    /// Respuesta exitosa de inicio de sesión idéntica a SAP Service Layer.
    /// </summary>
    public class LoginResponseDto
    {
        public string SessionId { get; set; } = string.Empty;
        public string Version { get; set; } = "10.0";
        public int SessionTimeout { get; set; } = 30; // Minutos
    }

    /// <summary>
    /// Representación interna de la sesión activa del usuario.
    /// </summary>
    public class UserSession
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
        public string CompanyDB { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? AuditUser { get; set; }
        public string? AuditApp { get; set; }
        public string ExecutionMode { get; set; } = "Direct"; // "Direct" (usuario propio) o "ServicePool" (cuenta compartida)
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime LastActivityUtc { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAtUtc { get; set; }
        public string? ClientIp { get; set; }

        public bool IsExpired => DateTime.UtcNow > ExpiresAtUtc;
    }

    /// <summary>
    /// Formato estándar de error compatible con SAP Service Layer.
    /// </summary>
    public class ServiceLayerErrorResponse
    {
        public ServiceLayerErrorDetail Error { get; set; } = new();

        public static ServiceLayerErrorResponse Create(int code, string message, string lang = "es-es")
        {
            return new ServiceLayerErrorResponse
            {
                Error = new ServiceLayerErrorDetail
                {
                    Code = code,
                    Message = new ServiceLayerErrorMessage
                    {
                        Lang = lang,
                        Value = message
                    }
                }
            };
        }
    }

    public class ServiceLayerErrorDetail
    {
        public int Code { get; set; }
        public ServiceLayerErrorMessage Message { get; set; } = new();
    }

    public class ServiceLayerErrorMessage
    {
        public string Lang { get; set; } = "es-es";
        public string Value { get; set; } = string.Empty;
    }
}
