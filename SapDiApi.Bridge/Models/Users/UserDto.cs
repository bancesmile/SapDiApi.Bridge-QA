using System.Text.Json.Serialization;

namespace SapDiApi.Bridge.Models.Users
{
    /// <summary>
    /// Representación de un Usuario en SAP Business One (OUSR / Definiciones de Usuarios)
    /// </summary>
    public class UserDto
    {
        /// <summary>
        /// Clave interna numérica del usuario en SAP (USERID / InternalKey).
        /// </summary>
        [JsonPropertyName("InternalKey")]
        public int InternalKey { get; set; }

        /// <summary>
        /// Código del usuario (USER_CODE). E.g: "manager", "creditos08".
        /// </summary>
        [JsonPropertyName("UserCode")]
        public string UserCode { get; set; } = string.Empty;

        /// <summary>
        /// Nombre completo o descriptivo del usuario (U_NAME).
        /// </summary>
        [JsonPropertyName("UserName")]
        public string? UserName { get; set; }

        /// <summary>
        /// Indica si el usuario es Superusuario ("tYES" / "tNO").
        /// </summary>
        [JsonPropertyName("Superuser")]
        public string? Superuser { get; set; } = "tNO";

        /// <summary>
        /// Indica si el usuario tiene acceso móvil ("tYES" / "tNO").
        /// </summary>
        [JsonPropertyName("MobileUser")]
        public string? MobileUser { get; set; } = "tNO";

        /// <summary>
        /// Indica si la cuenta de usuario está bloqueada ("tYES" / "tNO").
        /// </summary>
        [JsonPropertyName("Locked")]
        public string? Locked { get; set; } = "tNO";

        /// <summary>
        /// Grupo de valores predeterminados (DfltsGroup / Defaults).
        /// </summary>
        [JsonPropertyName("Defaults")]
        public string? Defaults { get; set; }

        #region Pestaña General

        /// <summary>
        /// Cuenta de usuario vinculada de Microsoft Windows (Domain / WindowsUserName).
        /// </summary>
        [JsonPropertyName("WindowsUserName")]
        public string? WindowsUserName { get; set; }

        /// <summary>
        /// ID del empleado vinculado en SAP (OHEM / empID).
        /// </summary>
        [JsonPropertyName("EmployeeId")]
        public int? EmployeeId { get; set; }

        /// <summary>
        /// Nombre del empleado vinculado.
        /// </summary>
        [JsonPropertyName("EmployeeName")]
        public string? EmployeeName { get; set; }

        /// <summary>
        /// Dirección de correo electrónico principal (E_Mail / eMail).
        /// </summary>
        [JsonPropertyName("eMail")]
        public string? Email { get; set; }

        /// <summary>
        /// Número de teléfono móvil (Mobile / MobilePhoneNumber).
        /// </summary>
        [JsonPropertyName("MobilePhoneNumber")]
        public string? MobilePhoneNumber { get; set; }

        /// <summary>
        /// Número de fax (Fax / FaxNumber).
        /// </summary>
        [JsonPropertyName("FaxNumber")]
        public string? FaxNumber { get; set; }

        /// <summary>
        /// ID del dispositivo móvil asignado (MobileID / MobileDeviceId).
        /// </summary>
        [JsonPropertyName("MobileDeviceId")]
        public string? MobileDeviceId { get; set; }

        /// <summary>
        /// Observaciones / Comentarios del usuario (Notes / Remarks).
        /// </summary>
        [JsonPropertyName("Remarks")]
        public string? Remarks { get; set; }

        /// <summary>
        /// Código de la sucursal asignada (Branch / OBPL / BPLId).
        /// </summary>
        [JsonPropertyName("Branch")]
        public int? Branch { get; set; }

        /// <summary>
        /// Nombre de la sucursal asignada.
        /// </summary>
        [JsonPropertyName("BranchName")]
        public string? BranchName { get; set; }

        /// <summary>
        /// Código del departamento asignado (Department / OUDP).
        /// </summary>
        [JsonPropertyName("Department")]
        public int? Department { get; set; }

        /// <summary>
        /// Nombre del departamento asignado.
        /// </summary>
        [JsonPropertyName("DepartmentName")]
        public string? DepartmentName { get; set; }

        /// <summary>
        /// Grupo de usuarios asignado (Group / BoUserGroup / ug_Regular).
        /// </summary>
        [JsonPropertyName("Group")]
        public string? Group { get; set; }

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

        #endregion

        #region Pestaña Servicios & Visualizar

        /// <summary>
        /// Código del idioma preferido en SAP (LanguageCode). E.g: "ln_Spanish_La", "ln_English".
        /// </summary>
        [JsonPropertyName("LanguageCode")]
        public string? LanguageCode { get; set; }

        /// <summary>
        /// Período de bloqueo de pantalla en minutos (ScreenLockTime / LockTime).
        /// </summary>
        [JsonPropertyName("ScreenLockTime")]
        public int? ScreenLockTime { get; set; }

        /// <summary>
        /// Máximo descuento general permitido (MaxDiscountGeneral).
        /// </summary>
        [JsonPropertyName("MaxDiscountGeneral")]
        public double? MaxDiscountGeneral { get; set; }

        /// <summary>
        /// Máximo descuento en ventas permitido (MaxDiscountSales).
        /// </summary>
        [JsonPropertyName("MaxDiscountSales")]
        public double? MaxDiscountSales { get; set; }

        /// <summary>
        /// Máximo descuento en compras permitido (MaxDiscountPurchase).
        /// </summary>
        [JsonPropertyName("MaxDiscountPurchase")]
        public double? MaxDiscountPurchase { get; set; }

        #endregion

        #region Campos de Usuario (UDFs - Facturación Electrónica y Custom)

        /// <summary>
        /// FE - Establecimiento asignado al usuario.
        /// </summary>
        [JsonPropertyName("U_Establecimiento")]
        public string? U_Establecimiento { get; set; }

        /// <summary>
        /// FE - ¿Es posible visualizar DTE de otros usuarios? ("Y" / "N").
        /// </summary>
        [JsonPropertyName("U_visualizar_todos_DTE")]
        public string? U_visualizar_todos_DTE { get; set; } = "N";

        /// <summary>
        /// FE - Múltiples Establecimientos ("Y" / "N").
        /// </summary>
        [JsonPropertyName("U_MultiEst")]
        public string? U_MultiEst { get; set; } = "N";

        /// <summary>
        /// FE - ¿Mostrar mensaje al enviar documento? ("Y" / "N").
        /// </summary>
        [JsonPropertyName("U_MensajeEnvioDocto")]
        public string? U_MensajeEnvioDocto { get; set; } = "N";

        /// <summary>
        /// FE - ¿Activar log? ("Y" / "N").
        /// </summary>
        [JsonPropertyName("U_ActivarLog")]
        public string? U_ActivarLog { get; set; } = "N";

        /// <summary>
        /// FE - ¿Guardar XML? ("Y" / "N").
        /// </summary>
        [JsonPropertyName("U_ActivarXML")]
        public string? U_ActivarXML { get; set; } = "N";

        /// <summary>
        /// Diccionario con otros campos de usuario UDFs dinámicos.
        /// </summary>
        [JsonPropertyName("UserFields")]
        public Dictionary<string, object?>? UserFields { get; set; }

        #endregion

        #region Metadatos de Auditoría y Última Conexión

        /// <summary>
        /// Fecha del último cierre de sesión (yyyy-MM-dd).
        /// </summary>
        [JsonPropertyName("LastLogoutDate")]
        public string? LastLogoutDate { get; set; }

        /// <summary>
        /// Hora del último inicio de sesión (HH:mm:ss).
        /// </summary>
        [JsonPropertyName("LastLoginTime")]
        public string? LastLoginTime { get; set; }

        /// <summary>
        /// Hora del último cierre de sesión (HH:mm:ss).
        /// </summary>
        [JsonPropertyName("LastLogoutTime")]
        public string? LastLogoutTime { get; set; }

        /// <summary>
        /// Hora del último cambio de contraseña (HH:mm:ss).
        /// </summary>
        [JsonPropertyName("LastPasswordChangeTime")]
        public string? LastPasswordChangeTime { get; set; }

        /// <summary>
        /// Código de usuario que realizó el último cambio de contraseña.
        /// </summary>
        [JsonPropertyName("LastPasswordChangedBy")]
        public string? LastPasswordChangedBy { get; set; }

        #endregion

        #region Colección de Permisos (Opcional bajo demanda)

        /// <summary>
        /// Permisos asignados al usuario (sólo se incluye si se solicita explícitamente con includePermissions=true).
        /// </summary>
        [JsonPropertyName("UserPermission")]
        public List<UserPermissionDto>? UserPermissions { get; set; }

        #endregion
    }

    /// <summary>
    /// Detalle de un permiso puntual asignado en SAP Business One (USR3 / UserPermission).
    /// </summary>
    public class UserPermissionDto
    {
        [JsonPropertyName("UserCode")]
        public int UserCode { get; set; }

        [JsonPropertyName("PermissionID")]
        public string PermissionID { get; set; } = string.Empty;

        [JsonPropertyName("Permission")]
        public string Permission { get; set; } = "boper_Full";
    }
}
