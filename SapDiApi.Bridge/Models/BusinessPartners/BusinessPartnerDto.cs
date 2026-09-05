using System.Text.Json.Serialization;

namespace SapDiApi.Bridge.Models.BusinessPartners
{
    /// <summary>
    /// Modelo completo de Socio de Negocio (Business Partner / OCRD) compatible con SAP Service Layer y DI API.
    /// </summary>
    public class BusinessPartnerDto
    {
        public string CardCode { get; set; } = string.Empty;
        public string CardName { get; set; } = string.Empty;
        public string CardType { get; set; } = "cSupplier"; // cCustomer, cSupplier, cLead
        public int GroupCode { get; set; } = 100;
        public int? Series { get; set; }
        public string? CardForeignName { get; set; }
        public string? AdditionalID { get; set; }
        public string? FederalTaxID { get; set; }
        public string? UnifiedFederalTaxID { get; set; }

        public string? Phone1 { get; set; }
        public string? Phone2 { get; set; }
        public string? Cellular { get; set; }
        public string? EmailAddress { get; set; }
        public string? Website { get; set; }
        public string? Notes { get; set; }
        public string? FreeText { get; set; }

        public string Currency { get; set; } = "USD";
        public decimal CurrentAccountBalance { get; set; } = 0.0m;
        public decimal OpenOrdersBalance { get; set; } = 0.0m;
        public decimal OpenDeliveryNotesBalance { get; set; } = 0.0m;
        public decimal CreditLimit { get; set; } = 0.0m;
        public decimal MaxCommitment { get; set; } = 0.0m;
        public decimal DiscountPercent { get; set; } = 0.0m;
        public int PayTermsGrpCode { get; set; } = -1;
        public int PriceListNum { get; set; } = -1;

        public string? Address { get; set; }
        public string? City { get; set; }
        public string? County { get; set; }
        public string? Country { get; set; }
        public string? BillToState { get; set; }
        public string? ShipToState { get; set; }
        public string? BilltoDefault { get; set; }
        public string? ShipToDefault { get; set; }

        public string? VatLiable { get; set; } = "vLiable";
        public string? VatGroupLatinAmerica { get; set; }
        public string? DebitorAccount { get; set; }
        public string? SubjectToWithholdingTax { get; set; } = "boNO";
        public string? AccrualCriteria { get; set; } = "tNO";

        public string? HouseBank { get; set; }
        public string? HouseBankCountry { get; set; }
        public string? HouseBankAccount { get; set; }
        public string? HouseBankBranch { get; set; }
        public string? HouseBankIBAN { get; set; }
        public string? DefaultAccount { get; set; }
        public string? DefaultBankCode { get; set; }

        public int? AttachmentEntry { get; set; }

        public string Valid { get; set; } = "tYES";
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public string Frozen { get; set; } = "tNO";
        public DateTime? FrozenFrom { get; set; }
        public DateTime? FrozenTo { get; set; }

        public DateTime? CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }

        // Campos de Usuario (UDF) dinámicos de SAP Business One (U_Regimen, U_NIT, U_PEP, U_TipoC, etc.)
        [JsonExtensionData]
        public Dictionary<string, object?> UserFields { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        // Colecciones relacionadas
        public List<BPAddressDto> BPAddresses { get; set; } = new();
        public List<ContactEmployeeDto> ContactEmployees { get; set; } = new();
        public List<BPBankAccountDto> BPBankAccounts { get; set; } = new();
        public List<BPPaymentMethodDto> BPPaymentMethods { get; set; } = new();
        public List<BPWithholdingTaxDto> BPWithholdingTaxCollection { get; set; } = new();
    }

    public class BPAddressDto
    {
        public string AddressName { get; set; } = string.Empty;
        public string? Street { get; set; }
        public string? Block { get; set; }
        public string? City { get; set; }
        public string? County { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Country { get; set; }
        public string AddressType { get; set; } = "bo_BillTo"; // bo_BillTo o bo_ShipTo
        public string? TaxCode { get; set; }
        public string? BPCode { get; set; }
        public int RowNum { get; set; }
        public string? TaasEnabled { get; set; } = "tYES";
    }

    public class ContactEmployeeDto
    {
        public int InternalCode { get; set; }
        public string? CardCode { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? EmailAddress { get; set; }
        [JsonPropertyName("E_Mail")]
        public string? E_Mail
        {
            get => EmailAddress;
            set => EmailAddress = value;
        }
        public string? Phone1 { get; set; }
        public string? Position { get; set; }
        public string? MobilePhone { get; set; }

        // Campos de usuario a nivel de contacto (ej: U_Area, U_Tipo)
        [JsonExtensionData]
        public Dictionary<string, object?> UserFields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class BPBankAccountDto
    {
        public string? BPCode { get; set; }
        public string? BankCode { get; set; }
        public string? AccountNumber { get; set; }
        [JsonPropertyName("AccountNo")]
        public string? AccountNo
        {
            get => AccountNumber;
            set => AccountNumber = value;
        }
        public string? AccountName { get; set; }
        public string? Branch { get; set; }
        public string? Country { get; set; }
        public string? IBAN { get; set; }
        public string? BICSwiftCode { get; set; }
        public int? ISRType { get; set; }

        public string? UserNo1 { get; set; }
        public string? UserNo2 { get; set; }
        public string? UserNo3 { get; set; }
        public string? UserNo4 { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object?> UserFields { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class BPPaymentMethodDto
    {
        public string? BPCode { get; set; }
        public string PaymentMethodCode { get; set; } = string.Empty;
        public int RowNumber { get; set; }
    }

    public class BPWithholdingTaxDto
    {
        public string? WTCode { get; set; }
        public string? WTName { get; set; }
        public decimal Rate { get; set; }
    }

    public class BusinessPartnerFilterDto
    {
        public string? Search { get; set; }
        public string? CardType { get; set; }
        public decimal? MinBalance { get; set; }
        public decimal? MaxBalance { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
