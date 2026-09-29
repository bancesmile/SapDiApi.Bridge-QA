using System.Text.Json.Serialization;

namespace SapDiApi.Bridge.Models.Companies
{
    /// <summary>
    /// Modelo de Sociedad / Empresa SAP registrada en el catálogo maestro.
    /// </summary>
    public class CompanyDto
    {
        /// <summary>
        /// ID numérico secuencial público para clientes externos (ej: 1, 2, 42).
        /// </summary>
        [JsonPropertyName("CompanyId")]
        public int CompanyId { get; set; }

        /// <summary>
        /// Código amigable o slug de la empresa (ej: "DIST_NORTE", "SUC_01").
        /// </summary>
        [JsonPropertyName("CompanyCode")]
        public string CompanyCode { get; set; } = string.Empty;

        /// <summary>
        /// Nombre descriptivo / Razón social de la empresa (cmpName en SAP).
        /// </summary>
        [JsonPropertyName("CompanyName")]
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>
        /// Nombre real de la base de datos física / esquema en SAP HANA (dbName en SAP).
        /// </summary>
        [JsonPropertyName("SapDatabase")]
        public string SapDatabase { get; set; } = string.Empty;

        /// <summary>
        /// Localización de la empresa (ej: "MX", "CL", "GT", "SV").
        /// </summary>
        [JsonPropertyName("Localization")]
        public string? Localization { get; set; }

        /// <summary>
        /// Indica si la sociedad está activa para operaciones en BridgeSap.
        /// </summary>
        [JsonPropertyName("IsActive")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Fecha de registro en el catálogo.
        /// </summary>
        [JsonPropertyName("CreatedAt")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Fecha de última sincronización o actualización.
        /// </summary>
        [JsonPropertyName("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO simplificado y seguro para exponer listados a terceros sin revelar nombres físicos de bases de datos.
    /// </summary>
    public class PublicCompanyDto
    {
        /// <summary>
        /// ID numérico de la empresa para enviar en encabezado `X-Company-Id`.
        /// </summary>
        [JsonPropertyName("CompanyId")]
        public int CompanyId { get; set; }

        /// <summary>
        /// Código de la empresa para enviar en encabezado `X-Company-Code` o `X-Company-Id`.
        /// </summary>
        [JsonPropertyName("CompanyCode")]
        public string CompanyCode { get; set; } = string.Empty;

        /// <summary>
        /// Nombre o Razón Social de la empresa.
        /// </summary>
        [JsonPropertyName("CompanyName")]
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>
        /// Localización (país).
        /// </summary>
        [JsonPropertyName("Localization")]
        public string? Localization { get; set; }

        /// <summary>
        /// Estado activo/inactivo.
        /// </summary>
        [JsonPropertyName("IsActive")]
        public bool IsActive { get; set; }
    }
}
