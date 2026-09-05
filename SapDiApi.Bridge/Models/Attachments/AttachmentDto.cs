using System.Text.Json.Serialization;

namespace SapDiApi.Bridge.Models.Attachments
{
    /// <summary>
    /// Modelo de Anexo / Adjunto (Attachments2 / OATC) compatible con SAP Business One y Service Layer.
    /// </summary>
    public class AttachmentDto
    {
        [JsonPropertyName("AbsoluteEntry")]
        public int? AbsoluteEntry { get; set; }

        [JsonPropertyName("AttachmentEntry")]
        public int? AttachmentEntry
        {
            get => AbsoluteEntry;
            set => AbsoluteEntry = value;
        }

        [JsonPropertyName("Attachments2_Lines")]
        public List<AttachmentLineDto> Lines { get; set; } = new();
    }

    public class AttachmentLineDto
    {
        public string? SourcePath { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? FileExtension { get; set; }
        public string? FreeText { get; set; }
        public string? Override { get; set; } = "tYES";
        public string? CopyToTargetDoc { get; set; } = "tNO";

        [JsonPropertyName("U_TipoDoc")]
        public string? U_TipoDoc { get; set; }

        [JsonPropertyName("U_PCV")]
        public string? U_PCV { get; set; }

        [JsonPropertyName("U_Inmueble")]
        public string? U_Inmueble { get; set; }

        [JsonPropertyName("U_Docs")]
        public string? U_Docs { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object?> UserFields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
