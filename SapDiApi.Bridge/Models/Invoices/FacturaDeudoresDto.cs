namespace SapDiApi.Bridge.Models.Invoices
{
    public class FacturaDeudoresDto
    {
        //public string? DocType { get; set; }
        //public string? HandWritten { get; set; }
        //public string? Printed { get; set; }
        public DateTime? DocDate { get; set; }
        public DateTime? DocDueDate { get; set; }
        public string? CardCode { get; set; }
        public string? CardName { get; set; }
        public string? Address { get; set; }
        // public decimal ? DocTotal { get; set; }
        public string? DocCurrency { get; set; }
        // public string? DocRate { get; set; }
        public string? Comments { get; set; }
        // public string JournalMemo { get; set; }
        public int Series { get; set; }
        public DateTime TaxDate { get; set; }
        public string PayToCode { get; set; } = string.Empty;
        public string U_Nit { get; set; } = string.Empty;
        public string? U_Nombre { get; set; }
        public string? U_FE_Correos { get; set; }
        public string? U_FE_Status { get; set; }
        public string? U_TipoDoctoSAT { get; set; }
        public string? U_DoctoFiscal { get; set; }
        public string? U_FE_Establecimiento { get; set; }
        public string? U_Direccion { get; set; }
        public string? U_Inmueble { get; set; }
        public string? U_Convenio { get; set; }
        public string? DocNum { get; set; }
        public string? DocEntry { get; set; }
        public List<InvoiceDocumentLinesDto> DocumentLines { get; set; } = new List<InvoiceDocumentLinesDto>();
        public InvoiceTaxExtensionDto TaxExtension { get; set; } = new InvoiceTaxExtensionDto();

    }

    public class InvoiceDocumentLinesDto
    {
        public int LineNum { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemDescription { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public double Price { get; set; }
        // public decimal? PriceAfterVAT { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string CostingCode { get; set; } = string.Empty;
        public string TaxCode { get; set; } = string.Empty;
        public string U_Tipo { get; set; } = string.Empty;
        public string U_Inmueble { get; set; } = string.Empty;

    }
    public class InvoiceTaxExtensionDto
    {
        public string StreetB { get; set; } = string.Empty;
        public string CityB { get; set; } = string.Empty;
        public string CountyB { get; set; } = string.Empty;
        //public string StateB { get; set; }
        //public string CountryB { get; set; }
    }
}
