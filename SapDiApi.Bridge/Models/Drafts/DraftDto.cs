using System.Text.Json.Serialization;

namespace SapDiApi.Bridge.Models.Drafts
{
    /// <summary>
    /// Representa un Documento Preliminar / Borrador en SAP Business One (ODRF) compatible con Service Layer.
    /// </summary>
    public class DraftDto
    {
        /// <summary>
        /// Clave interna del borrador (ODRF.DocEntry).
        /// </summary>
        [JsonPropertyName("DocEntry")]
        public int DocEntry { get; set; }

        /// <summary>
        /// Número visible del documento borrador (ODRF.DocNum).
        /// </summary>
        [JsonPropertyName("DocNum")]
        public int DocNum { get; set; }

        /// <summary>
        /// Tipo de documento (dDocument_Items, dDocument_Service, etc.).
        /// </summary>
        [JsonPropertyName("DocType")]
        public string? DocType { get; set; }

        /// <summary>
        /// Tipo de documento SAP base (ej: oPurchaseOrders, oInvoices, oOrders, etc.).
        /// </summary>
        [JsonPropertyName("DocObjectCode")]
        public string? DocObjectCode { get; set; }

        /// <summary>
        /// Fecha de contabilización del documento (YYYY-MM-DD).
        /// </summary>
        [JsonPropertyName("DocDate")]
        public string? DocDate { get; set; }

        /// <summary>
        /// Fecha de vencimiento del documento (YYYY-MM-DD).
        /// </summary>
        [JsonPropertyName("DocDueDate")]
        public string? DocDueDate { get; set; }

        /// <summary>
        /// Fecha del documento / fiscal (YYYY-MM-DD).
        /// </summary>
        [JsonPropertyName("TaxDate")]
        public string? TaxDate { get; set; }

        /// <summary>
        /// Código del Socio de Negocio (Proveedor / Cliente).
        /// </summary>
        [JsonPropertyName("CardCode")]
        public string? CardCode { get; set; }

        /// <summary>
        /// Razón social o nombre del Socio de Negocio.
        /// </summary>
        [JsonPropertyName("CardName")]
        public string? CardName { get; set; }

        /// <summary>
        /// Dirección de entrega/facturación.
        /// </summary>
        [JsonPropertyName("Address")]
        public string? Address { get; set; }

        /// <summary>
        /// Dirección complementaria o fiscal.
        /// </summary>
        [JsonPropertyName("Address2")]
        public string? Address2 { get; set; }

        /// <summary>
        /// Número de referencia del socio de negocio / factura del proveedor.
        /// </summary>
        [JsonPropertyName("NumAtCard")]
        public string? NumAtCard { get; set; }

        /// <summary>
        /// Total del documento en moneda local.
        /// </summary>
        [JsonPropertyName("DocTotal")]
        public decimal DocTotal { get; set; }

        /// <summary>
        /// Total del documento en moneda del sistema.
        /// </summary>
        [JsonPropertyName("DocTotalSys")]
        public decimal? DocTotalSys { get; set; }

        /// <summary>
        /// Total del documento en moneda extranjera.
        /// </summary>
        [JsonPropertyName("DocTotalFc")]
        public decimal? DocTotalFc { get; set; }

        /// <summary>
        /// Moneda del documento (ej: QTZ, USD, EUR).
        /// </summary>
        [JsonPropertyName("DocCurrency")]
        public string? DocCurrency { get; set; }

        /// <summary>
        /// Tipo de cambio del documento.
        /// </summary>
        [JsonPropertyName("DocRate")]
        public decimal? DocRate { get; set; }

        /// <summary>
        /// Total de impuestos (IVA) del documento.
        /// </summary>
        [JsonPropertyName("VatSum")]
        public decimal? VatSum { get; set; }

        /// <summary>
        /// Porcentaje de descuento global.
        /// </summary>
        [JsonPropertyName("DiscountPercent")]
        public decimal? DiscountPercent { get; set; }

        /// <summary>
        /// Comentarios u observaciones del documento.
        /// </summary>
        [JsonPropertyName("Comments")]
        public string? Comments { get; set; }

        /// <summary>
        /// Nota o referencia de diario contable.
        /// </summary>
        [JsonPropertyName("JournalMemo")]
        public string? JournalMemo { get; set; }

        /// <summary>
        /// Código de condición de pago (PaymentGroupCode).
        /// </summary>
        [JsonPropertyName("PaymentGroupCode")]
        public int? PaymentGroupCode { get; set; }

        /// <summary>
        /// Serie de numeración de SAP.
        /// </summary>
        [JsonPropertyName("Series")]
        public int? Series { get; set; }

        /// <summary>
        /// ID del anexo vinculado (Attachments2.AbsoluteEntry).
        /// </summary>
        [JsonPropertyName("AttachmentEntry")]
        public int? AttachmentEntry { get; set; }

        /// <summary>
        /// Estado del documento preliminar (bost_Open, bost_Close, etc.).
        /// </summary>
        [JsonPropertyName("DocumentStatus")]
        public string? DocumentStatus { get; set; }

        /// <summary>
        /// Estado de autorización del borrador (dasApproved, dasPending, dasRejected, etc.).
        /// </summary>
        [JsonPropertyName("AuthorizationStatus")]
        public string? AuthorizationStatus { get; set; }

        /// <summary>
        /// Indica si el documento está confirmado (tYES / tNO).
        /// </summary>
        [JsonPropertyName("Confirmed")]
        public string? Confirmed { get; set; }

        /// <summary>
        /// Código de usuario creador en SAP (UserSign / OUSR.USERID).
        /// </summary>
        [JsonPropertyName("UserSign")]
        public int? UserSign { get; set; }

        /// <summary>
        /// NIT o RFC fiscal del socio de negocio.
        /// </summary>
        [JsonPropertyName("FederalTaxID")]
        public string? FederalTaxID { get; set; }

        /// <summary>
        /// Fecha de creación del borrador en el sistema.
        /// </summary>
        [JsonPropertyName("CreationDate")]
        public string? CreationDate { get; set; }

        /// <summary>
        /// Hora de creación del borrador.
        /// </summary>
        [JsonPropertyName("DocTime")]
        public string? DocTime { get; set; }

        /// <summary>
        /// Líneas de detalle de artículos / servicios del borrador (DRF1).
        /// </summary>
        [JsonPropertyName("DocumentLines")]
        public List<DraftDocumentLineDto> DocumentLines { get; set; } = new();

        /// <summary>
        /// Extensión de información fiscal.
        /// </summary>
        [JsonPropertyName("TaxExtension")]
        public DraftTaxExtensionDto? TaxExtension { get; set; }

        /// <summary>
        /// Extensión de direcciones de entrega y facturación.
        /// </summary>
        [JsonPropertyName("AddressExtension")]
        public DraftAddressExtensionDto? AddressExtension { get; set; }

        /// <summary>
        /// Cuotas de pago asociadas al borrador.
        /// </summary>
        [JsonPropertyName("DocumentInstallments")]
        public List<DraftInstallmentDto> DocumentInstallments { get; set; } = new();

        /// <summary>
        /// Campos definidos por el usuario (UDFs) a nivel de encabezado (U_...).
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object?> UserFields { get; set; } = new();
    }

    /// <summary>
    /// Línea de artículo o servicio de un documento borrador (DRF1).
    /// </summary>
    public class DraftDocumentLineDto
    {
        /// <summary>
        /// Número de línea secuencial (0-indexed).
        /// </summary>
        [JsonPropertyName("LineNum")]
        public int LineNum { get; set; }

        /// <summary>
        /// Código del artículo o servicio (DRF1.ItemCode).
        /// </summary>
        [JsonPropertyName("ItemCode")]
        public string? ItemCode { get; set; }

        /// <summary>
        /// Descripción del artículo o servicio (DRF1.Dscription).
        /// </summary>
        [JsonPropertyName("ItemDescription")]
        public string? ItemDescription { get; set; }

        /// <summary>
        /// Cantidad solicitada.
        /// </summary>
        [JsonPropertyName("Quantity")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// Precio unitario sin impuestos.
        /// </summary>
        [JsonPropertyName("Price")]
        public decimal Price { get; set; }

        /// <summary>
        /// Precio unitario con impuestos incluidos.
        /// </summary>
        [JsonPropertyName("PriceAfterVAT")]
        public decimal? PriceAfterVAT { get; set; }

        /// <summary>
        /// Moneda del precio de línea.
        /// </summary>
        [JsonPropertyName("Currency")]
        public string? Currency { get; set; }

        /// <summary>
        /// Porcentaje de descuento de línea.
        /// </summary>
        [JsonPropertyName("DiscountPercent")]
        public decimal? DiscountPercent { get; set; }

        /// <summary>
        /// Código de almacén de entrada o salida (DRF1.WhsCode).
        /// </summary>
        [JsonPropertyName("WarehouseCode")]
        public string? WarehouseCode { get; set; }

        /// <summary>
        /// Cuenta contable asociada a la línea.
        /// </summary>
        [JsonPropertyName("AccountCode")]
        public string? AccountCode { get; set; }

        /// <summary>
        /// Centro de costo nivel 1 / Norma de reparto (OOCR).
        /// </summary>
        [JsonPropertyName("CostingCode")]
        public string? CostingCode { get; set; }

        /// <summary>
        /// Centro de costo nivel 2.
        /// </summary>
        [JsonPropertyName("CostingCode2")]
        public string? CostingCode2 { get; set; }

        /// <summary>
        /// Centro de costo nivel 3.
        /// </summary>
        [JsonPropertyName("CostingCode3")]
        public string? CostingCode3 { get; set; }

        /// <summary>
        /// Centro de costo nivel 4.
        /// </summary>
        [JsonPropertyName("CostingCode4")]
        public string? CostingCode4 { get; set; }

        /// <summary>
        /// Centro de costo nivel 5.
        /// </summary>
        [JsonPropertyName("CostingCode5")]
        public string? CostingCode5 { get; set; }

        /// <summary>
        /// Código de proyecto financiero asociado.
        /// </summary>
        [JsonPropertyName("ProjectCode")]
        public string? ProjectCode { get; set; }

        /// <summary>
        /// Código o grupo de impuesto (ej: IVA, EXE).
        /// </summary>
        [JsonPropertyName("TaxCode")]
        public string? TaxCode { get; set; }

        /// <summary>
        /// Grupo de impuesto (VatGroup).
        /// </summary>
        [JsonPropertyName("VatGroup")]
        public string? VatGroup { get; set; }

        /// <summary>
        /// Total neto de la línea.
        /// </summary>
        [JsonPropertyName("LineTotal")]
        public decimal LineTotal { get; set; }

        /// <summary>
        /// Total bruto con impuestos incluidos.
        /// </summary>
        [JsonPropertyName("GrossTotal")]
        public decimal? GrossTotal { get; set; }

        /// <summary>
        /// Total bruto en moneda del sistema.
        /// </summary>
        [JsonPropertyName("GrossTotalSC")]
        public decimal? GrossTotalSC { get; set; }

        /// <summary>
        /// Monto total de impuestos de la línea.
        /// </summary>
        [JsonPropertyName("TaxTotal")]
        public decimal? TaxTotal { get; set; }

        /// <summary>
        /// Porcentaje de impuesto por fila.
        /// </summary>
        [JsonPropertyName("TaxPercentagePerRow")]
        public decimal? TaxPercentagePerRow { get; set; }

        /// <summary>
        /// Unidad de medida (DRF1.unitMsr).
        /// </summary>
        [JsonPropertyName("MeasureUnit")]
        public string? MeasureUnit { get; set; }

        /// <summary>
        /// Código de unidad de medida.
        /// </summary>
        [JsonPropertyName("UoMCode")]
        public string? UoMCode { get; set; }

        /// <summary>
        /// Texto libre en la línea del documento.
        /// </summary>
        [JsonPropertyName("FreeText")]
        public string? FreeText { get; set; }

        /// <summary>
        /// Estado de la línea (bost_Open, bost_Close).
        /// </summary>
        [JsonPropertyName("LineStatus")]
        public string? LineStatus { get; set; }

        /// <summary>
        /// Campos de usuario definidos en la línea (U_...).
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object?> UserFields { get; set; } = new();
    }

    /// <summary>
    /// Información fiscal complementaria del borrador.
    /// </summary>
    public class DraftTaxExtensionDto
    {
        [JsonPropertyName("DocEntry")]
        public int? DocEntry { get; set; }

        [JsonPropertyName("TaxId0")]
        public string? TaxId0 { get; set; }

        [JsonPropertyName("StreetS")]
        public string? StreetS { get; set; }

        [JsonPropertyName("CityS")]
        public string? CityS { get; set; }

        [JsonPropertyName("StateS")]
        public string? StateS { get; set; }

        [JsonPropertyName("CountryS")]
        public string? CountryS { get; set; }

        [JsonPropertyName("MainUsage")]
        public string? MainUsage { get; set; }
    }

    /// <summary>
    /// Información de direcciones de entrega y facturación del borrador.
    /// </summary>
    public class DraftAddressExtensionDto
    {
        [JsonPropertyName("DocEntry")]
        public int? DocEntry { get; set; }

        [JsonPropertyName("ShipToStreet")]
        public string? ShipToStreet { get; set; }

        [JsonPropertyName("ShipToCity")]
        public string? ShipToCity { get; set; }

        [JsonPropertyName("ShipToState")]
        public string? ShipToState { get; set; }

        [JsonPropertyName("ShipToCountry")]
        public string? ShipToCountry { get; set; }

        [JsonPropertyName("BillToStreet")]
        public string? BillToStreet { get; set; }

        [JsonPropertyName("BillToCity")]
        public string? BillToCity { get; set; }

        [JsonPropertyName("BillToState")]
        public string? BillToState { get; set; }

        [JsonPropertyName("BillToCountry")]
        public string? BillToCountry { get; set; }
    }

    /// <summary>
    /// Cuota o plazo de pago del borrador.
    /// </summary>
    public class DraftInstallmentDto
    {
        [JsonPropertyName("InstallmentId")]
        public int InstallmentId { get; set; }

        [JsonPropertyName("DueDate")]
        public string? DueDate { get; set; }

        [JsonPropertyName("Percentage")]
        public decimal? Percentage { get; set; }

        [JsonPropertyName("Total")]
        public decimal? Total { get; set; }
    }

    /// <summary>
    /// Filtros de búsqueda para borradores de documentos en SAP.
    /// </summary>
    public class DraftFilterDto
    {
        /// <summary>
        /// Filtrar por tipo de objeto base (ej: "22" para Órdenes de Compra, "13" Facturas, etc.).
        /// </summary>
        public string? DocObjectCode { get; set; }

        /// <summary>
        /// Filtrar por código de socio de negocio.
        /// </summary>
        public string? CardCode { get; set; }

        /// <summary>
        /// Filtrar por estado del documento (bost_Open, bost_Close).
        /// </summary>
        public string? DocumentStatus { get; set; }

        /// <summary>
        /// Fecha inicial de contabilización (YYYY-MM-DD).
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// Fecha final de contabilización (YYYY-MM-DD).
        /// </summary>
        public DateTime? ToDate { get; set; }
    }
}
