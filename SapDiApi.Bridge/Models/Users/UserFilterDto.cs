using System.Text.Json.Serialization;

namespace SapDiApi.Bridge.Models.Users
{
    /// <summary>
    /// Filtros de búsqueda para el listado de usuarios de SAP.
    /// </summary>
    public class UserFilterDto
    {
        /// <summary>
        /// Búsqueda por texto (código de usuario o nombre completo).
        /// </summary>
        public string? Search { get; set; }

        /// <summary>
        /// Filtrar por estado bloqueado ("tYES" / "tNO" o "Y" / "N").
        /// </summary>
        public string? Locked { get; set; }

        /// <summary>
        /// Filtrar por Superusuario ("tYES" / "tNO" o "Y" / "N").
        /// </summary>
        public string? Superuser { get; set; }

        /// <summary>
        /// Filtrar por sucursal asignada (BPLId / Branch).
        /// </summary>
        public int? Branch { get; set; }

        /// <summary>
        /// Filtrar por departamento asignado.
        /// </summary>
        public int? Department { get; set; }

        /// <summary>
        /// Número de página para paginación (base 1).
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Cantidad de registros por página (por defecto 50, máx 500).
        /// </summary>
        public int PageSize { get; set; } = 50;
    }

    /// <summary>
    /// Datos para crear un nuevo usuario en SAP Business One.
    /// </summary>
    public class CreateUserDto
    {
        /// <summary>
        /// Código de inicio de sesión del usuario (obligatorio).
        /// </summary>
        [JsonPropertyName("UserCode")]
        public string UserCode { get; set; } = string.Empty;

        /// <summary>
        /// Nombre completo del usuario.
        /// </summary>
        [JsonPropertyName("UserName")]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Contraseña inicial del usuario (obligatorio para la creación).
        /// </summary>
        [JsonPropertyName("UserPassword")]
        public string UserPassword { get; set; } = string.Empty;

        /// <summary>
        /// ¿Es Superusuario? ("tYES" / "tNO").
        /// </summary>
        [JsonPropertyName("Superuser")]
        public string? Superuser { get; set; } = "tNO";

        /// <summary>
        /// ¿Es usuario móvil? ("tYES" / "tNO").
        /// </summary>
        [JsonPropertyName("MobileUser")]
        public string? MobileUser { get; set; } = "tNO";

        /// <summary>
        /// ¿Cuenta bloqueada? ("tYES" / "tNO").
        /// </summary>
        [JsonPropertyName("Locked")]
        public string? Locked { get; set; } = "tNO";

        /// <summary>
        /// Correo electrónico del usuario.
        /// </summary>
        [JsonPropertyName("eMail")]
        public string? Email { get; set; }

        /// <summary>
        /// Teléfono móvil.
        /// </summary>
        [JsonPropertyName("MobilePhoneNumber")]
        public string? MobilePhoneNumber { get; set; }

        /// <summary>
        /// ID de dispositivo móvil.
        /// </summary>
        [JsonPropertyName("MobileDeviceId")]
        public string? MobileDeviceId { get; set; }

        /// <summary>
        /// Empleado vinculado (empID).
        /// </summary>
        [JsonPropertyName("EmployeeId")]
        public int? EmployeeId { get; set; }

        /// <summary>
        /// Sucursal (BPLId).
        /// </summary>
        [JsonPropertyName("Branch")]
        public int? Branch { get; set; }

        /// <summary>
        /// Departamento.
        /// </summary>
        [JsonPropertyName("Department")]
        public int? Department { get; set; }

        /// <summary>
        /// Grupo de valores predeterminados (DfltsGroup).
        /// </summary>
        [JsonPropertyName("Defaults")]
        public string? Defaults { get; set; }

        /// <summary>
        /// La clave de acceso nunca vence ("tYES" / "tNO").
        /// </summary>
        [JsonPropertyName("PasswordNeverExpires")]
        public string? PasswordNeverExpires { get; set; } = "tNO";

        /// <summary>
        /// Modificar clave de acceso en la próxima conexión ("tYES" / "tNO").
        /// </summary>
        [JsonPropertyName("ChangePasswordNextLogon")]
        public string? ChangePasswordNextLogon { get; set; } = "tNO";

        /// <summary>
        /// Idioma preferido (LanguageCode).
        /// </summary>
        [JsonPropertyName("LanguageCode")]
        public string? LanguageCode { get; set; }

        /// <summary>
        /// Observaciones / Comentarios.
        /// </summary>
        [JsonPropertyName("Remarks")]
        public string? Remarks { get; set; }

        #region Campos de Usuario (UDFs)

        [JsonPropertyName("U_Establecimiento")]
        public string? U_Establecimiento { get; set; }

        [JsonPropertyName("U_visualizar_todos_DTE")]
        public string? U_visualizar_todos_DTE { get; set; }

        [JsonPropertyName("U_MultiEst")]
        public string? U_MultiEst { get; set; }

        [JsonPropertyName("U_MensajeEnvioDocto")]
        public string? U_MensajeEnvioDocto { get; set; }

        [JsonPropertyName("U_ActivarLog")]
        public string? U_ActivarLog { get; set; }

        [JsonPropertyName("U_ActivarXML")]
        public string? U_ActivarXML { get; set; }

        [JsonPropertyName("UserFields")]
        public Dictionary<string, object?>? UserFields { get; set; }

        #endregion
    }

    /// <summary>
    /// Datos para actualizar un usuario existente en SAP Business One.
    /// </summary>
    public class UpdateUserDto
    {
        /// <summary>
        /// Nombre descriptivo del usuario.
        /// </summary>
        [JsonPropertyName("UserName")]
        public string? UserName { get; set; }

        /// <summary>
        /// ¿Es Superusuario? ("tYES" / "tNO").
        /// </summary>
        [JsonPropertyName("Superuser")]
        public string? Superuser { get; set; }

        /// <summary>
        /// ¿Es usuario móvil? ("tYES" / "tNO").
        /// </summary>
        [JsonPropertyName("MobileUser")]
        public string? MobileUser { get; set; }

        /// <summary>
        /// ¿Cuenta bloqueada? ("tYES" / "tNO").
        /// </summary>
        [JsonPropertyName("Locked")]
        public string? Locked { get; set; }

        /// <summary>
        /// Correo electrónico del usuario.
        /// </summary>
        [JsonPropertyName("eMail")]
        public string? Email { get; set; }

        /// <summary>
        /// Teléfono móvil.
        /// </summary>
        [JsonPropertyName("MobilePhoneNumber")]
        public string? MobilePhoneNumber { get; set; }

        /// <summary>
        /// ID de dispositivo móvil.
        /// </summary>
        [JsonPropertyName("MobileDeviceId")]
        public string? MobileDeviceId { get; set; }

        /// <summary>
        /// Empleado vinculado (empID).
        /// </summary>
        [JsonPropertyName("EmployeeId")]
        public int? EmployeeId { get; set; }

        /// <summary>
        /// Sucursal (BPLId).
        /// </summary>
        [JsonPropertyName("Branch")]
        public int? Branch { get; set; }

        /// <summary>
        /// Departamento.
        /// </summary>
        [JsonPropertyName("Department")]
        public int? Department { get; set; }

        /// <summary>
        /// Grupo de valores predeterminados (DfltsGroup).
        /// </summary>
        [JsonPropertyName("Defaults")]
        public string? Defaults { get; set; }

        /// <summary>
        /// La clave de acceso nunca vence ("tYES" / "tNO").
        /// </summary>
        [JsonPropertyName("PasswordNeverExpires")]
        public string? PasswordNeverExpires { get; set; }

        /// <summary>
        /// Modificar clave de acceso en la próxima conexión ("tYES" / "tNO").
        /// </summary>
        [JsonPropertyName("ChangePasswordNextLogon")]
        public string? ChangePasswordNextLogon { get; set; }

        /// <summary>
        /// Idioma preferido (LanguageCode).
        /// </summary>
        [JsonPropertyName("LanguageCode")]
        public string? LanguageCode { get; set; }

        /// <summary>
        /// Observaciones / Comentarios.
        /// </summary>
        [JsonPropertyName("Remarks")]
        public string? Remarks { get; set; }

        #region Campos de Usuario (UDFs)

        [JsonPropertyName("U_Establecimiento")]
        public string? U_Establecimiento { get; set; }

        [JsonPropertyName("U_visualizar_todos_DTE")]
        public string? U_visualizar_todos_DTE { get; set; }

        [JsonPropertyName("U_MultiEst")]
        public string? U_MultiEst { get; set; }

        [JsonPropertyName("U_MensajeEnvioDocto")]
        public string? U_MensajeEnvioDocto { get; set; }

        [JsonPropertyName("U_ActivarLog")]
        public string? U_ActivarLog { get; set; }

        [JsonPropertyName("U_ActivarXML")]
        public string? U_ActivarXML { get; set; }

        [JsonPropertyName("UserFields")]
        public Dictionary<string, object?>? UserFields { get; set; }

        #endregion
    }

    /// <summary>
    /// Solicitud de cambio o reset de contraseña de usuario.
    /// </summary>
    public class ChangeUserPasswordDto
    {
        /// <summary>
        /// Nueva contraseña a establecer.
        /// </summary>
        [JsonPropertyName("NewPassword")]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Exigir cambio de clave en la próxima conexión ("tYES" / "tNO" o "Y" / "N").
        /// </summary>
        [JsonPropertyName("ChangePasswordNextLogon")]
        public string? ChangePasswordNextLogon { get; set; }

        /// <summary>
        /// Indicar si la clave nunca vence ("tYES" / "tNO" o "Y" / "N").
        /// </summary>
        [JsonPropertyName("PasswordNeverExpires")]
        public string? PasswordNeverExpires { get; set; }
    }
}
