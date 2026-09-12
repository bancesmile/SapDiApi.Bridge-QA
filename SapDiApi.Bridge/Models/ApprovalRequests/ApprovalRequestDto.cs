using System.Text.Json.Serialization;

namespace SapDiApi.Bridge.Models.ApprovalRequests
{
    /// <summary>
    /// Representa una Solicitud de Aprobación de SAP Business One (OWDD) compatible con Service Layer.
    /// </summary>
    public class ApprovalRequestDto
    {
        /// <summary>
        /// Código identificador único de la solicitud de aprobación (OWDD.WddCode).
        /// </summary>
        [JsonPropertyName("Code")]
        public int Code { get; set; }

        /// <summary>
        /// ID del modelo o plantilla de autorización (OWDD.WtmCode).
        /// </summary>
        [JsonPropertyName("ApprovalTemplatesID")]
        public int? ApprovalTemplatesID { get; set; }

        /// <summary>
        /// Tipo de objeto del documento base (ej: "22" para Órdenes de Compra, "17" para Pedidos de Venta).
        /// </summary>
        [JsonPropertyName("ObjectType")]
        public string? ObjectType { get; set; }

        /// <summary>
        /// Indica si el documento está en estado preliminar / borrador ("Y" o "N").
        /// </summary>
        [JsonPropertyName("IsDraft")]
        public string? IsDraft { get; set; }

        /// <summary>
        /// DocEntry del documento generado en SAP una vez aprobada la solicitud.
        /// </summary>
        [JsonPropertyName("ObjectEntry")]
        public int? ObjectEntry { get; set; }

        /// <summary>
        /// Estado general de la solicitud de aprobación (arsApproved, arsPending, arsNotApproved, arsCanceled, arsGenerated, etc.).
        /// </summary>
        [JsonPropertyName("Status")]
        public string? Status { get; set; }

        /// <summary>
        /// Observaciones o comentarios generales de la solicitud.
        /// </summary>
        [JsonPropertyName("Remarks")]
        public string? Remarks { get; set; }

        /// <summary>
        /// Etapa o fase actual en el flujo de aprobación (OWDD.CurrStep).
        /// </summary>
        [JsonPropertyName("CurrentStage")]
        public int? CurrentStage { get; set; }

        /// <summary>
        /// ID del usuario creador / solicitante del documento (OWDD.OwnerID).
        /// </summary>
        [JsonPropertyName("OriginatorID")]
        public int? OriginatorID { get; set; }

        /// <summary>
        /// Fecha de creación de la solicitud (formato YYYY-MM-DD).
        /// </summary>
        [JsonPropertyName("CreationDate")]
        public string? CreationDate { get; set; }

        /// <summary>
        /// Hora de creación de la solicitud (formato HH:mm:ss).
        /// </summary>
        [JsonPropertyName("CreationTime")]
        public string? CreationTime { get; set; }

        /// <summary>
        /// Clave única (DocEntry) del borrador asociado en ODRF.
        /// </summary>
        [JsonPropertyName("DraftEntry")]
        public int? DraftEntry { get; set; }

        /// <summary>
        /// Tipo de borrador de SAP (ODRF.ObjType / OWDD.DraftType).
        /// </summary>
        [JsonPropertyName("DraftType")]
        public string? DraftType { get; set; }

        /// <summary>
        /// Líneas de etapas y usuarios asignados para la autorización (WDD1).
        /// </summary>
        [JsonPropertyName("ApprovalRequestLines")]
        public List<ApprovalRequestLineDto> ApprovalRequestLines { get; set; } = new();

        /// <summary>
        /// Registro histórico de decisiones tomadas por los autorizadores (WDD2).
        /// </summary>
        [JsonPropertyName("ApprovalRequestDecisions")]
        public List<ApprovalRequestDecisionDto> ApprovalRequestDecisions { get; set; } = new();
    }

    /// <summary>
    /// Línea de etapa y autorizador en la solicitud de aprobación (WDD1).
    /// </summary>
    public class ApprovalRequestLineDto
    {
        /// <summary>
        /// Código de la etapa de autorización (WDD1.StepCode).
        /// </summary>
        [JsonPropertyName("StageCode")]
        public int StageCode { get; set; }

        /// <summary>
        /// ID del usuario autorizador asignado en esta etapa (WDD1.UserID).
        /// </summary>
        [JsonPropertyName("UserID")]
        public int UserID { get; set; }

        /// <summary>
        /// Estado de la decisión del autorizador (ardApproved, ardPending, ardNotApproved, etc. o 'Y', 'W', 'N').
        /// </summary>
        [JsonPropertyName("Status")]
        public string? Status { get; set; }

        /// <summary>
        /// Comentarios u observaciones emitidas por el autorizador.
        /// </summary>
        [JsonPropertyName("Remarks")]
        public string? Remarks { get; set; }

        /// <summary>
        /// Fecha de última actualización de la etapa.
        /// </summary>
        [JsonPropertyName("UpdateDate")]
        public string? UpdateDate { get; set; }

        /// <summary>
        /// Hora de última actualización de la etapa.
        /// </summary>
        [JsonPropertyName("UpdateTime")]
        public string? UpdateTime { get; set; }

        /// <summary>
        /// Fecha de creación de la etapa.
        /// </summary>
        [JsonPropertyName("CreationDate")]
        public string? CreationDate { get; set; }

        /// <summary>
        /// Hora de creación de la etapa.
        /// </summary>
        [JsonPropertyName("CreationTime")]
        public string? CreationTime { get; set; }
    }

    /// <summary>
    /// Registro de decisión tomada por un autorizador (WDD2 / ApprovalRequestDecisions).
    /// </summary>
    public class ApprovalRequestDecisionDto
    {
        /// <summary>
        /// ID del autorizador que toma la decisión.
        /// </summary>
        [JsonPropertyName("ApproverID")]
        public int? ApproverID { get; set; }

        /// <summary>
        /// Nombre de usuario en SAP para autenticar y firmar la autorización.
        /// </summary>
        [JsonPropertyName("ApproverUserName")]
        public string? ApproverUserName { get; set; }

        /// <summary>
        /// Contraseña del usuario autorizador en SAP para validar la firma.
        /// </summary>
        [JsonPropertyName("ApproverPassword")]
        public string? ApproverPassword { get; set; }

        /// <summary>
        /// Estado de la decisión (ardApproved, ardNotApproved, ardPending, etc.).
        /// </summary>
        [JsonPropertyName("Status")]
        public string? Status { get; set; }

        /// <summary>
        /// Observaciones o justificación de la decisión tomada.
        /// </summary>
        [JsonPropertyName("Remarks")]
        public string? Remarks { get; set; }

        /// <summary>
        /// Fecha en que se tomó la decisión.
        /// </summary>
        [JsonPropertyName("UpdateDate")]
        public string? UpdateDate { get; set; }

        /// <summary>
        /// Hora en que se tomó la decisión.
        /// </summary>
        [JsonPropertyName("UpdateTime")]
        public string? UpdateTime { get; set; }
    }

    /// <summary>
    /// Payload de actualización / resolución de una solicitud de aprobación (PATCH / PUT).
    /// </summary>
    public class UpdateApprovalRequestDto
    {
        /// <summary>
        /// Estado general de la aprobación ('Y' - Aprobado, 'N' - Rechazado, 'W' - Pendiente).
        /// </summary>
        [JsonPropertyName("Status")]
        public string? Status { get; set; }

        /// <summary>
        /// Observaciones generales de la solicitud.
        /// </summary>
        [JsonPropertyName("Remarks")]
        public string? Remarks { get; set; }

        /// <summary>
        /// Etapa actual de autorización.
        /// </summary>
        [JsonPropertyName("CurrentStage")]
        public int? CurrentStage { get; set; }

        /// <summary>
        /// Indica si es borrador ('Y' o 'N').
        /// </summary>
        [JsonPropertyName("IsDraft")]
        public string? IsDraft { get; set; }

        /// <summary>
        /// Tipo de objeto del documento base (ej: "22").
        /// </summary>
        [JsonPropertyName("ObjectType")]
        public string? ObjectType { get; set; }

        /// <summary>
        /// ID del solicitante u originador.
        /// </summary>
        [JsonPropertyName("OriginatorID")]
        public int? OriginatorID { get; set; }

        /// <summary>
        /// Entrada del documento generado tras la aprobación (opcional).
        /// </summary>
        [JsonPropertyName("ObjectEntry")]
        public int? ObjectEntry { get; set; }

        /// <summary>
        /// Lista de líneas de aprobación a actualizar.
        /// </summary>
        [JsonPropertyName("ApprovalRequestLines")]
        public List<ApprovalRequestLineDto>? ApprovalRequestLines { get; set; }

        /// <summary>
        /// Decisiones firmadas por los autorizadores.
        /// </summary>
        [JsonPropertyName("ApprovalRequestDecisions")]
        public List<ApprovalRequestDecisionDto>? ApprovalRequestDecisions { get; set; }
    }

    /// <summary>
    /// Parámetros de filtro para consultas y listados de solicitudes de aprobación.
    /// </summary>
    public class ApprovalRequestFilterDto
    {
        /// <summary>
        /// Filtrar por ID del usuario solicitante.
        /// </summary>
        public int? OriginatorID { get; set; }

        /// <summary>
        /// Filtrar por ID de un autorizador específico asignado en las líneas de aprobación.
        /// </summary>
        public int? UserID { get; set; }

        /// <summary>
        /// Filtrar por estado de la solicitud (arsApproved, arsPending, arsNotApproved, Y, W, N).
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// Filtrar por DocEntry del borrador preliminar.
        /// </summary>
        public int? DraftEntry { get; set; }

        /// <summary>
        /// Filtrar por tipo de documento (ej: "22" para Órdenes de Compra).
        /// </summary>
        public string? ObjectType { get; set; }

        /// <summary>
        /// Fecha inicial de creación (formato YYYY-MM-DD).
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// Fecha final de creación (formato YYYY-MM-DD).
        /// </summary>
        public DateTime? ToDate { get; set; }
    }
}
