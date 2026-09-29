using System.Runtime.InteropServices;
using SAPbobsCOM;
using SapDiApi.Bridge.Models.ApprovalRequests;
using SapDiApi.Bridge.Models.Attachments;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.BusinessPartners;
using SapDiApi.Bridge.Models.Drafts;
using SapDiApi.Bridge.Models.Users;
using SapDiApi.Bridge.Models.Companies;
using SapDiApi.Bridge.Infrastructure.Common;
using SapDiApi.Bridge.Models.Sap;

namespace SapDiApi.Bridge.Services.Sap
{
    public class SapDiApiConnector : ISapDiApiConnector
    {
        private readonly ISapCompanyPool _companyPool;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SapDiApiConnector> _logger;

        public SapDiApiConnector(
            ISapCompanyPool companyPool,
            IConfiguration configuration,
            ILogger<SapDiApiConnector> logger)
        {
            _companyPool = companyPool;
            _configuration = configuration;
            _logger = logger;
        }

        public SapConnectionInfo BuildConnectionInfo(string companyDb, string userName, string password)
        {
            return new SapConnectionInfo(companyDb, userName, password)
            {
                Server = _configuration["SapSettings:Server"] ?? "127.0.0.1",
                LicenseServer = _configuration["SapSettings:LicenseServer"] ?? "127.0.0.1:30000",
                SLDServer = _configuration["SapSettings:SLDServer"],
                DbServerType = _configuration["SapSettings:DbServerType"] ?? "dst_HANADB",
                DbUserName = _configuration["SapSettings:DbUserName"],
                DbPassword = _configuration["SapSettings:DbPassword"],
                UseTrusted = _configuration.GetValue<bool>("SapSettings:UseTrusted", false)
            };
        }

        public (bool Connected, string ErrorMessage) Connect(UserSession session, string password)
        {
            var connInfo = BuildConnectionInfo(session.CompanyDB, session.UserName, password);

            try
            {
                var connected = _companyPool.ExecuteAsync(connInfo, company =>
                {
                    _logger.LogInformation("Conexión exitosa a SAP DI API: {Key} (Versión SAP: {Version})",
                        connInfo.GenerarClaveConexion(), company.Version);
                    return Task.FromResult(true);
                }).GetAwaiter().GetResult();

                return (connected, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo en la conexión a SAP DI API para usuario {User} en BD {DB}", session.UserName, session.CompanyDB);
                return (false, ex.Message);
            }
        }

        public async Task<BusinessPartnerDto?> GetBusinessPartnerAsync(UserSession session, string cardCode)
        {
            var connInfo = BuildConnectionInfo(session.CompanyDB, session.UserName, session.Password);

            _logger.LogInformation(
                "DI API: Consultando Socio de Negocio '{CardCode}' en SAP | DB: {DB} | Usuario SAP: {User} | Operador: {AuditUser} | Modo: {Mode}",
                cardCode, session.CompanyDB, session.UserName, session.AuditUser ?? "N/A", session.ExecutionMode);

            return await _companyPool.ExecuteAsync(connInfo, company =>
            {
                SAPbobsCOM.BusinessPartners? oBusinessPartner = null;
                try
                {
                    oBusinessPartner = (SAPbobsCOM.BusinessPartners)company.GetBusinessObject(BoObjectTypes.oBusinessPartners);

                    if (!oBusinessPartner.GetByKey(cardCode))
                    {
                        _logger.LogWarning("Socio de negocio '{CardCode}' no encontrado en SAP.", cardCode);
                        return Task.FromResult<BusinessPartnerDto?>(null);
                    }

                    var cardTypeStr = oBusinessPartner.CardType switch
                    {
                        BoCardTypes.cCustomer => "cCustomer",
                        BoCardTypes.cSupplier => "cSupplier",
                        _ => "cLead"
                    };

                    var bp = new BusinessPartnerDto
                    {
                        CardCode = oBusinessPartner.CardCode ?? string.Empty,
                        CardName = oBusinessPartner.CardName ?? string.Empty,
                        CardType = cardTypeStr,
                        GroupCode = oBusinessPartner.GroupCode,
                        CardForeignName = oBusinessPartner.CardForeignName,
                        AdditionalID = oBusinessPartner.AdditionalID,
                        FederalTaxID = oBusinessPartner.FederalTaxID,
                        UnifiedFederalTaxID = oBusinessPartner.UnifiedFederalTaxID,

                        Phone1 = oBusinessPartner.Phone1,
                        Phone2 = oBusinessPartner.Phone2,
                        Cellular = oBusinessPartner.Cellular,
                        EmailAddress = oBusinessPartner.EmailAddress,
                        Website = oBusinessPartner.Website,
                        Notes = oBusinessPartner.Notes,
                        FreeText = oBusinessPartner.FreeText,

                        Currency = oBusinessPartner.Currency ?? "USD",
                        CurrentAccountBalance = (decimal)oBusinessPartner.CurrentAccountBalance,
                        OpenOrdersBalance = (decimal)oBusinessPartner.OpenOrdersBalance,
                        OpenDeliveryNotesBalance = (decimal)oBusinessPartner.OpenDeliveryNotesBalance,
                        CreditLimit = (decimal)oBusinessPartner.CreditLimit,
                        MaxCommitment = (decimal)oBusinessPartner.MaxCommitment,
                        DiscountPercent = (decimal)oBusinessPartner.DiscountPercent,
                        PayTermsGrpCode = oBusinessPartner.PayTermsGrpCode,
                        PriceListNum = oBusinessPartner.PriceListNum,

                        VatLiable = oBusinessPartner.VatLiable == BoVatStatus.vLiable ? "vLiable" : "vExempted",
                        VatGroupLatinAmerica = oBusinessPartner.VatGroupLatinAmerica,
                        DebitorAccount = oBusinessPartner.DebitorAccount,

                        City = oBusinessPartner.City,
                        Country = oBusinessPartner.Country,
                        BillToState = oBusinessPartner.BillToState,
                        ShipToState = oBusinessPartner.ShipToDefault,

                        HouseBank = oBusinessPartner.HouseBank,
                        HouseBankCountry = oBusinessPartner.HouseBankCountry,
                        HouseBankAccount = oBusinessPartner.HouseBankAccount,
                        HouseBankBranch = oBusinessPartner.HouseBankBranch,
                        HouseBankIBAN = oBusinessPartner.HouseBankIBAN,

                        Valid = oBusinessPartner.Valid == BoYesNoEnum.tYES ? "tYES" : "tNO",
                        ValidFrom = CleanSapDate(oBusinessPartner.ValidFrom),
                        ValidTo = CleanSapDate(oBusinessPartner.ValidTo),
                        Frozen = oBusinessPartner.Frozen == BoYesNoEnum.tYES ? "tYES" : "tNO",
                        FrozenFrom = CleanSapDate(oBusinessPartner.FrozenFrom),
                        FrozenTo = CleanSapDate(oBusinessPartner.FrozenTo),

                        CreateDate = CleanSapDate(oBusinessPartner.CreateDate),
                        UpdateDate = CleanSapDate(oBusinessPartner.UpdateDate)
                    };

                    // Mapeo dinámico de Campos de Usuario (UDF) de SAP
                    try
                    {
                        var bpUserFields = oBusinessPartner.UserFields;
                        var userFields = bpUserFields?.Fields;
                        if (userFields != null)
                        {
                            for (int i = 0; i < userFields.Count; i++)
                            {
                                Field? field = null;
                                try
                                {
                                    field = userFields.Item(i);
                                    if (field != null)
                                    {
                                        var fieldName = field.Name.StartsWith("U_") ? field.Name : $"U_{field.Name}";
                                        var val = field.Value;
                                        bp.UserFields[fieldName] = (val is DateTime dtVal) ? CleanSapDate(dtVal) : val;
                                    }
                                }
                                finally
                                {
                                    ComHelper.Release(field);
                                }
                            }
                            ComHelper.Release(userFields);
                        }
                        ComHelper.Release(bpUserFields);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug("Aviso al mapear UserFields de BusinessPartners: {Message}", ex.Message);
                    }

                    // Mapeo de Direcciones (BPAddresses)
                    try
                    {
                        var addresses = oBusinessPartner.Addresses;
                        if (addresses != null)
                        {
                            for (int i = 0; i < addresses.Count; i++)
                            {
                                addresses.SetCurrentLine(i);
                                if (!string.IsNullOrEmpty(addresses.AddressName))
                                {
                                    bp.BPAddresses.Add(new BPAddressDto
                                    {
                                        AddressName = addresses.AddressName,
                                        Street = addresses.Street,
                                        Block = addresses.Block,
                                        City = addresses.City,
                                        State = addresses.State,
                                        Country = addresses.Country,
                                        ZipCode = addresses.ZipCode,
                                        AddressType = addresses.AddressType == BoAddressType.bo_BillTo ? "bo_BillTo" : "bo_ShipTo",
                                        TaxCode = addresses.TaxCode
                                    });
                                }
                            }
                            ComHelper.Release(addresses);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug("Aviso al mapear Direcciones: {Message}", ex.Message);
                    }

                    // Mapeo de Contactos (ContactEmployees)
                    try
                    {
                        var contacts = oBusinessPartner.ContactEmployees;
                        if (contacts != null)
                        {
                            for (int i = 0; i < contacts.Count; i++)
                            {
                                contacts.SetCurrentLine(i);
                                if (!string.IsNullOrEmpty(contacts.Name))
                                {
                                    var contactDto = new ContactEmployeeDto
                                    {
                                        InternalCode = contacts.InternalCode,
                                        Name = contacts.Name,
                                        FirstName = contacts.FirstName,
                                        LastName = contacts.LastName,
                                        EmailAddress = contacts.E_Mail,
                                        Phone1 = contacts.Phone1,
                                        Position = contacts.Position,
                                        MobilePhone = contacts.MobilePhone
                                    };

                                    // UDFs a nivel de Contacto (U_Area, U_Tipo)
                                    try
                                    {
                                        var contactUF = contacts.UserFields;
                                        var contactUdfs = contactUF?.Fields;
                                        if (contactUdfs != null)
                                        {
                                            for (int u = 0; u < contactUdfs.Count; u++)
                                            {
                                                Field? f = null;
                                                try
                                                {
                                                    f = contactUdfs.Item(u);
                                                    if (f != null)
                                                    {
                                                        var fname = f.Name.StartsWith("U_") ? f.Name : $"U_{f.Name}";
                                                        contactDto.UserFields[fname] = f.Value;
                                                    }
                                                }
                                                finally
                                                {
                                                    ComHelper.Release(f);
                                                }
                                            }
                                            ComHelper.Release(contactUdfs);
                                        }
                                        ComHelper.Release(contactUF);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogDebug("Aviso UDF Contacto: {Message}", ex.Message);
                                    }

                                    bp.ContactEmployees.Add(contactDto);
                                }
                            }
                            ComHelper.Release(contacts);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug("Aviso al mapear Contactos: {Message}", ex.Message);
                    }

                    // Mapeo de Cuentas Bancarias (BPBankAccounts)
                    try
                    {
                        var bpBankAccounts = oBusinessPartner.BPBankAccounts;
                        if (bpBankAccounts != null)
                        {
                            for (int i = 0; i < bpBankAccounts.Count; i++)
                            {
                                bpBankAccounts.SetCurrentLine(i);
                                if (!string.IsNullOrEmpty(bpBankAccounts.BankCode) || !string.IsNullOrEmpty(bpBankAccounts.AccountNo))
                                {
                                    bp.BPBankAccounts.Add(new BPBankAccountDto
                                    {
                                        BankCode = bpBankAccounts.BankCode,
                                        AccountNumber = bpBankAccounts.AccountNo,
                                        AccountName = bpBankAccounts.AccountName,
                                        Branch = bpBankAccounts.Branch,
                                        Country = bpBankAccounts.Country,
                                        IBAN = bpBankAccounts.IBAN
                                    });
                                }
                            }
                            ComHelper.Release(bpBankAccounts);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug("Aviso al mapear BPBankAccounts: {Message}", ex.Message);
                    }

                    // Mapeo de Métodos de Pago (BPPaymentMethods)
                    try
                    {
                        var paymentMethods = oBusinessPartner.BPPaymentMethods;
                        if (paymentMethods != null)
                        {
                            for (int i = 0; i < paymentMethods.Count; i++)
                            {
                                paymentMethods.SetCurrentLine(i);
                                if (!string.IsNullOrEmpty(paymentMethods.PaymentMethodCode))
                                {
                                    bp.BPPaymentMethods.Add(new BPPaymentMethodDto
                                    {
                                        PaymentMethodCode = paymentMethods.PaymentMethodCode
                                    });
                                }
                            }
                            ComHelper.Release(paymentMethods);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug("Aviso al mapear BPPaymentMethods: {Message}", ex.Message);
                    }

                    return Task.FromResult<BusinessPartnerDto?>(bp);
                }
                finally
                {
                    ComHelper.Release(oBusinessPartner);
                }
            });
        }

        public async Task<(bool Success, string CardCode, string? ErrorMessage)> CreateBusinessPartnerAsync(UserSession session, BusinessPartnerDto dto)
        {
            var connInfo = BuildConnectionInfo(session.CompanyDB, session.UserName, session.Password);

            _logger.LogInformation(
                "DI API: Creando nuevo Socio de Negocio '{CardCode}' ('{CardName}') en SAP | DB: {DB} | Usuario SAP: {User} | Operador: {AuditUser}",
                dto.CardCode, dto.CardName, session.CompanyDB, session.UserName, session.AuditUser ?? "N/A");

            return await _companyPool.ExecuteAsync(connInfo, company =>
            {
                SAPbobsCOM.BusinessPartners? oBusinessPartner = null;
                try
                {
                    oBusinessPartner = (SAPbobsCOM.BusinessPartners)company.GetBusinessObject(BoObjectTypes.oBusinessPartners);

                    // Propiedades Básicas
                    if (!string.IsNullOrWhiteSpace(dto.CardCode))
                        oBusinessPartner.CardCode = dto.CardCode;

                    oBusinessPartner.CardName = dto.CardName;

                    oBusinessPartner.CardType = dto.CardType?.ToUpperInvariant() switch
                    {
                        "C" or "CCUSTOMER" => BoCardTypes.cCustomer,
                        "S" or "CSUPPLIER" => BoCardTypes.cSupplier,
                        _ => BoCardTypes.cSupplier
                    };

                    if (dto.Series.HasValue && dto.Series.Value > 0)
                        oBusinessPartner.Series = dto.Series.Value;

                    if (!string.IsNullOrWhiteSpace(dto.CardForeignName))
                        oBusinessPartner.CardForeignName = dto.CardForeignName;

                    if (!string.IsNullOrWhiteSpace(dto.AdditionalID))
                        oBusinessPartner.AdditionalID = dto.AdditionalID;

                    if (!string.IsNullOrWhiteSpace(dto.FederalTaxID))
                        oBusinessPartner.FederalTaxID = dto.FederalTaxID;

                    if (!string.IsNullOrWhiteSpace(dto.UnifiedFederalTaxID))
                        oBusinessPartner.UnifiedFederalTaxID = dto.UnifiedFederalTaxID;

                    if (!string.IsNullOrWhiteSpace(dto.Currency))
                        oBusinessPartner.Currency = dto.Currency;

                    if (dto.GroupCode > 0)
                        oBusinessPartner.GroupCode = dto.GroupCode;

                    if (!string.IsNullOrWhiteSpace(dto.Phone1))
                        oBusinessPartner.Phone1 = dto.Phone1;

                    if (!string.IsNullOrWhiteSpace(dto.Phone2))
                        oBusinessPartner.Phone2 = dto.Phone2;

                    if (!string.IsNullOrWhiteSpace(dto.Cellular))
                        oBusinessPartner.Cellular = dto.Cellular;

                    if (!string.IsNullOrWhiteSpace(dto.EmailAddress))
                        oBusinessPartner.EmailAddress = dto.EmailAddress;

                    if (!string.IsNullOrWhiteSpace(dto.Website))
                        oBusinessPartner.Website = dto.Website;

                    if (!string.IsNullOrWhiteSpace(dto.FreeText))
                        oBusinessPartner.FreeText = dto.FreeText;

                    if (!string.IsNullOrWhiteSpace(dto.Address))
                        oBusinessPartner.Address = dto.Address;

                    if (!string.IsNullOrWhiteSpace(dto.City))
                        oBusinessPartner.City = dto.City;

                    if (!string.IsNullOrWhiteSpace(dto.County))
                        oBusinessPartner.County = dto.County;

                    if (!string.IsNullOrWhiteSpace(dto.BillToState))
                        oBusinessPartner.BillToState = dto.BillToState;

                    if (!string.IsNullOrWhiteSpace(dto.BilltoDefault))
                        oBusinessPartner.BilltoDefault = dto.BilltoDefault;

                    if (dto.PayTermsGrpCode > 0)
                        oBusinessPartner.PayTermsGrpCode = dto.PayTermsGrpCode;

                    if (dto.PriceListNum > 0)
                        oBusinessPartner.PriceListNum = dto.PriceListNum;

                    if (!string.IsNullOrWhiteSpace(dto.VatGroupLatinAmerica))
                        oBusinessPartner.VatGroupLatinAmerica = dto.VatGroupLatinAmerica;

                    if (!string.IsNullOrWhiteSpace(dto.VatLiable))
                    {
                        oBusinessPartner.VatLiable = string.Equals(dto.VatLiable, "vLiable", StringComparison.OrdinalIgnoreCase)
                            ? BoVatStatus.vLiable
                            : BoVatStatus.vExempted;
                    }

                    // Configuración de Banco de la Casa
                    if (!string.IsNullOrWhiteSpace(dto.HouseBank))
                        oBusinessPartner.HouseBank = dto.HouseBank;

                    if (!string.IsNullOrWhiteSpace(dto.HouseBankCountry))
                        oBusinessPartner.HouseBankCountry = dto.HouseBankCountry;

                    if (!string.IsNullOrWhiteSpace(dto.HouseBankAccount))
                        oBusinessPartner.HouseBankAccount = dto.HouseBankAccount;

                    // Manejo de Propiedades (Properties1 - Properties64)
                    foreach (var udf in dto.UserFields)
                    {
                        if (udf.Key.StartsWith("Properties", StringComparison.OrdinalIgnoreCase) &&
                            int.TryParse(udf.Key.Substring(10), out int propIndex) && propIndex >= 1 && propIndex <= 64)
                        {
                            var propVal = udf.Value?.ToString()?.ToUpperInvariant();
                            oBusinessPartner.Properties[propIndex] = (propVal == "TYES" || propVal == "TRUE" || propVal == "1")
                                ? BoYesNoEnum.tYES
                                : BoYesNoEnum.tNO;
                        }
                    }

                    // Configuración de Retenciones
                    if (!string.IsNullOrWhiteSpace(dto.SubjectToWithholdingTax))
                    {
                        bool isWithholding = string.Equals(dto.SubjectToWithholdingTax, "boYES", StringComparison.OrdinalIgnoreCase) ||
                                             string.Equals(dto.SubjectToWithholdingTax, "tYES", StringComparison.OrdinalIgnoreCase);
                        oBusinessPartner.SubjectToWithholdingTax = isWithholding
                            ? (BoYesNoNoneEnum)BoYesNoEnum.tYES
                            : (BoYesNoNoneEnum)BoYesNoEnum.tNO;

                        if (isWithholding && dto.BPWithholdingTaxCollection != null)
                        {
                            foreach (var wt in dto.BPWithholdingTaxCollection)
                            {
                                if (!string.IsNullOrWhiteSpace(wt.WTCode))
                                {
                                    if (oBusinessPartner.BPWithholdingTax.Count > 0 && !string.IsNullOrEmpty(oBusinessPartner.BPWithholdingTax.WTCode))
                                    {
                                        oBusinessPartner.BPWithholdingTax.Add();
                                    }
                                    oBusinessPartner.BPWithholdingTax.SetCurrentLine(oBusinessPartner.BPWithholdingTax.Count - 1);
                                    oBusinessPartner.BPWithholdingTax.WTCode = wt.WTCode;
                                }
                            }
                        }
                    }

                    // Campos de Usuario (UDF) dinámicos
                    foreach (var kvp in dto.UserFields)
                    {
                        if (kvp.Key.StartsWith("Properties", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var fieldName = kvp.Key.StartsWith("U_") ? kvp.Key : $"U_{kvp.Key}";
                        try
                        {
                            if (kvp.Value != null && kvp.Value.ToString() != "NULL")
                            {
                                oBusinessPartner.UserFields.Fields.Item(fieldName).Value = kvp.Value.ToString();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug("Aviso al asignar UDF '{Field}': {Message}", fieldName, ex.Message);
                        }
                    }

                    // Agregar Direcciones (BPAddresses)
                    if (dto.BPAddresses != null && dto.BPAddresses.Count > 0)
                    {
                        foreach (var dir in dto.BPAddresses)
                        {
                            if (oBusinessPartner.Addresses.Count > 0 && !string.IsNullOrEmpty(oBusinessPartner.Addresses.AddressName))
                            {
                                oBusinessPartner.Addresses.Add();
                            }

                            oBusinessPartner.Addresses.SetCurrentLine(oBusinessPartner.Addresses.Count - 1);

                            if (!string.IsNullOrWhiteSpace(dir.AddressName))
                                oBusinessPartner.Addresses.AddressName = dir.AddressName;

                            if (!string.IsNullOrWhiteSpace(dir.Street))
                                oBusinessPartner.Addresses.Street = dir.Street;

                            if (!string.IsNullOrWhiteSpace(dir.Block))
                                oBusinessPartner.Addresses.Block = dir.Block;

                            if (!string.IsNullOrWhiteSpace(dir.ZipCode))
                                oBusinessPartner.Addresses.ZipCode = dir.ZipCode;

                            if (!string.IsNullOrWhiteSpace(dir.City))
                                oBusinessPartner.Addresses.City = dir.City;

                            if (!string.IsNullOrWhiteSpace(dir.County))
                                oBusinessPartner.Addresses.County = dir.County;

                            if (!string.IsNullOrWhiteSpace(dir.Country))
                                oBusinessPartner.Addresses.Country = dir.Country;

                            if (!string.IsNullOrWhiteSpace(dir.State))
                                oBusinessPartner.Addresses.State = dir.State;

                            if (!string.IsNullOrWhiteSpace(dir.TaxCode))
                                oBusinessPartner.Addresses.TaxCode = dir.TaxCode;

                            if (string.Equals(dir.AddressType, "bo_BillTo", StringComparison.OrdinalIgnoreCase))
                            {
                                oBusinessPartner.Addresses.AddressType = BoAddressType.bo_BillTo;
                                if (string.IsNullOrWhiteSpace(oBusinessPartner.BilltoDefault))
                                    oBusinessPartner.BilltoDefault = dir.AddressName;
                            }
                            else if (string.Equals(dir.AddressType, "bo_ShipTo", StringComparison.OrdinalIgnoreCase))
                            {
                                oBusinessPartner.Addresses.AddressType = BoAddressType.bo_ShipTo;
                                if (string.IsNullOrWhiteSpace(oBusinessPartner.ShipToDefault))
                                    oBusinessPartner.ShipToDefault = dir.AddressName;
                            }
                        }
                    }

                    // Agregar Contactos (ContactEmployees)
                    if (dto.ContactEmployees != null && dto.ContactEmployees.Count > 0)
                    {
                        foreach (var c in dto.ContactEmployees)
                        {
                            if (oBusinessPartner.ContactEmployees.Count > 0 && !string.IsNullOrEmpty(oBusinessPartner.ContactEmployees.Name))
                            {
                                oBusinessPartner.ContactEmployees.Add();
                            }

                            oBusinessPartner.ContactEmployees.SetCurrentLine(oBusinessPartner.ContactEmployees.Count - 1);

                            if (!string.IsNullOrWhiteSpace(c.Name))
                                oBusinessPartner.ContactEmployees.Name = c.Name;

                            if (!string.IsNullOrWhiteSpace(c.FirstName))
                                oBusinessPartner.ContactEmployees.FirstName = c.FirstName;

                            if (!string.IsNullOrWhiteSpace(c.LastName))
                                oBusinessPartner.ContactEmployees.LastName = c.LastName;

                            if (!string.IsNullOrWhiteSpace(c.Position))
                                oBusinessPartner.ContactEmployees.Position = c.Position;

                            if (!string.IsNullOrWhiteSpace(c.Phone1))
                                oBusinessPartner.ContactEmployees.Phone1 = c.Phone1;

                            if (!string.IsNullOrWhiteSpace(c.EmailAddress))
                                oBusinessPartner.ContactEmployees.E_Mail = c.EmailAddress;

                            // UDFs a nivel de Contacto
                            foreach (var cudf in c.UserFields)
                            {
                                var fname = cudf.Key.StartsWith("U_") ? cudf.Key : $"U_{cudf.Key}";
                                try
                                {
                                    if (cudf.Value != null)
                                    {
                                        oBusinessPartner.ContactEmployees.UserFields.Fields.Item(fname).Value = cudf.Value.ToString();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogDebug("Aviso UDF Contacto '{Field}': {Message}", fname, ex.Message);
                                }
                            }
                        }
                    }

                    // Agregar Cuentas Bancarias (BPBankAccounts)
                    if (dto.BPBankAccounts != null && dto.BPBankAccounts.Count > 0)
                    {
                        foreach (var bAcc in dto.BPBankAccounts)
                        {
                            if (oBusinessPartner.BPBankAccounts.Count > 0 && !string.IsNullOrEmpty(oBusinessPartner.BPBankAccounts.AccountNo))
                            {
                                oBusinessPartner.BPBankAccounts.Add();
                            }

                            oBusinessPartner.BPBankAccounts.SetCurrentLine(oBusinessPartner.BPBankAccounts.Count - 1);

                            if (!string.IsNullOrWhiteSpace(bAcc.AccountNumber))
                                oBusinessPartner.BPBankAccounts.AccountNo = bAcc.AccountNumber;

                            if (!string.IsNullOrWhiteSpace(bAcc.BankCode))
                                oBusinessPartner.BPBankAccounts.BankCode = bAcc.BankCode;

                            if (!string.IsNullOrWhiteSpace(bAcc.Country))
                                oBusinessPartner.BPBankAccounts.Country = bAcc.Country;

                            if (!string.IsNullOrWhiteSpace(bAcc.AccountName))
                                oBusinessPartner.BPBankAccounts.AccountName = bAcc.AccountName;

                            if (!string.IsNullOrWhiteSpace(bAcc.BICSwiftCode))
                                oBusinessPartner.BPBankAccounts.BICSwiftCode = bAcc.BICSwiftCode;

                            if (bAcc.ISRType.HasValue)
                                oBusinessPartner.BPBankAccounts.ISRType = bAcc.ISRType.Value;

                            if (!string.IsNullOrWhiteSpace(bAcc.UserNo1))
                                try { oBusinessPartner.BPBankAccounts.UserFields.Fields.Item("UsrNumber1").Value = bAcc.UserNo1; } catch { }
                            if (!string.IsNullOrWhiteSpace(bAcc.UserNo2))
                                try { oBusinessPartner.BPBankAccounts.UserFields.Fields.Item("UsrNumber2").Value = bAcc.UserNo2; } catch { }
                            if (!string.IsNullOrWhiteSpace(bAcc.UserNo3))
                                try { oBusinessPartner.BPBankAccounts.UserFields.Fields.Item("UsrNumber3").Value = bAcc.UserNo3; } catch { }
                        }

                        var lastAcc = dto.BPBankAccounts[dto.BPBankAccounts.Count - 1];
                        if (!string.IsNullOrWhiteSpace(lastAcc.AccountNumber))
                            oBusinessPartner.DefaultAccount = lastAcc.AccountNumber;
                        if (!string.IsNullOrWhiteSpace(lastAcc.BankCode))
                            oBusinessPartner.DefaultBankCode = lastAcc.BankCode;
                    }

                    // Agregar Métodos de Pago (BPPaymentMethods)
                    if (dto.BPPaymentMethods != null && dto.BPPaymentMethods.Count > 0)
                    {
                        foreach (var pm in dto.BPPaymentMethods)
                        {
                            if (!string.IsNullOrWhiteSpace(pm.PaymentMethodCode))
                            {
                                if (oBusinessPartner.BPPaymentMethods.Count > 0 && !string.IsNullOrEmpty(oBusinessPartner.BPPaymentMethods.PaymentMethodCode))
                                {
                                    oBusinessPartner.BPPaymentMethods.Add();
                                }
                                oBusinessPartner.BPPaymentMethods.SetCurrentLine(oBusinessPartner.BPPaymentMethods.Count - 1);
                                oBusinessPartner.BPPaymentMethods.PaymentMethodCode = pm.PaymentMethodCode.Trim();
                            }
                        }
                    }

                    // Anexo si aplica
                    if (dto.AttachmentEntry.HasValue && dto.AttachmentEntry.Value > 0)
                    {
                        oBusinessPartner.AttachmentEntry = dto.AttachmentEntry.Value;
                    }

                    // Guardar en SAP Business One
                    int addResult = oBusinessPartner.Add();

                    if (addResult == 0)
                    {
                        string createdCardCode = dto.CardCode;
                        if (string.IsNullOrWhiteSpace(createdCardCode))
                        {
                            company.GetNewObjectCode(out createdCardCode);
                        }

                        _logger.LogInformation("Socio de negocio '{CardCode}' creado exitosamente en SAP.", createdCardCode);
                        return Task.FromResult((true, createdCardCode, (string?)null));
                    }
                    else
                    {
                        company.GetLastError(out int lastErrorCode, out string lastErrorDescription);
                        _logger.LogError("Fallo al crear socio de negocio en SAP ({Code}): {Error}", lastErrorCode, lastErrorDescription);
                        return Task.FromResult((false, dto.CardCode, (string?)$"Error SAP ({lastErrorCode}): {lastErrorDescription}"));
                    }
                }
                finally
                {
                    ComHelper.Release(oBusinessPartner);
                }
            });
        }

        public async Task<(bool Success, string CardCode, string? ErrorMessage)> UpdateBusinessPartnerAsync(UserSession session, string cardCode, BusinessPartnerDto dto)
        {
            var connInfo = BuildConnectionInfo(session.CompanyDB, session.UserName, session.Password);

            _logger.LogInformation(
                "DI API: Actualizando Socio de Negocio '{CardCode}' en SAP | DB: {DB} | Usuario SAP: {User} | Operador: {AuditUser}",
                cardCode, session.CompanyDB, session.UserName, session.AuditUser ?? "N/A");

            return await _companyPool.ExecuteAsync(connInfo, company =>
            {
                SAPbobsCOM.BusinessPartners? oBusinessPartner = null;
                try
                {
                    oBusinessPartner = (SAPbobsCOM.BusinessPartners)company.GetBusinessObject(BoObjectTypes.oBusinessPartners);

                    if (!oBusinessPartner.GetByKey(cardCode))
                    {
                        return Task.FromResult((false, cardCode, (string?)$"El socio de negocio con código '{cardCode}' no existe en SAP."));
                    }

                    if (!string.IsNullOrWhiteSpace(dto.CardName))
                        oBusinessPartner.CardName = dto.CardName;

                    if (!string.IsNullOrWhiteSpace(dto.CardForeignName))
                        oBusinessPartner.CardForeignName = dto.CardForeignName;

                    if (!string.IsNullOrWhiteSpace(dto.AdditionalID))
                        oBusinessPartner.AdditionalID = dto.AdditionalID;

                    if (!string.IsNullOrWhiteSpace(dto.FederalTaxID))
                        oBusinessPartner.FederalTaxID = dto.FederalTaxID;

                    if (!string.IsNullOrWhiteSpace(dto.UnifiedFederalTaxID))
                        oBusinessPartner.UnifiedFederalTaxID = dto.UnifiedFederalTaxID;

                    if (!string.IsNullOrWhiteSpace(dto.Currency))
                        oBusinessPartner.Currency = dto.Currency;

                    if (dto.GroupCode > 0)
                        oBusinessPartner.GroupCode = dto.GroupCode;

                    if (!string.IsNullOrWhiteSpace(dto.Phone1))
                        oBusinessPartner.Phone1 = dto.Phone1;

                    if (!string.IsNullOrWhiteSpace(dto.Phone2))
                        oBusinessPartner.Phone2 = dto.Phone2;

                    if (!string.IsNullOrWhiteSpace(dto.Cellular))
                        oBusinessPartner.Cellular = dto.Cellular;

                    if (!string.IsNullOrWhiteSpace(dto.EmailAddress))
                        oBusinessPartner.EmailAddress = dto.EmailAddress;

                    if (!string.IsNullOrWhiteSpace(dto.Website))
                        oBusinessPartner.Website = dto.Website;

                    if (!string.IsNullOrWhiteSpace(dto.FreeText))
                        oBusinessPartner.FreeText = dto.FreeText;

                    if (!string.IsNullOrWhiteSpace(dto.Address))
                        oBusinessPartner.Address = dto.Address;

                    if (!string.IsNullOrWhiteSpace(dto.City))
                        oBusinessPartner.City = dto.City;

                    if (!string.IsNullOrWhiteSpace(dto.County))
                        oBusinessPartner.County = dto.County;

                    if (!string.IsNullOrWhiteSpace(dto.BillToState))
                        oBusinessPartner.BillToState = dto.BillToState;

                    if (dto.PayTermsGrpCode > 0)
                        oBusinessPartner.PayTermsGrpCode = dto.PayTermsGrpCode;

                    if (dto.PriceListNum > 0)
                        oBusinessPartner.PriceListNum = dto.PriceListNum;

                    if (!string.IsNullOrWhiteSpace(dto.VatGroupLatinAmerica))
                        oBusinessPartner.VatGroupLatinAmerica = dto.VatGroupLatinAmerica;

                    if (!string.IsNullOrWhiteSpace(dto.VatLiable))
                    {
                        oBusinessPartner.VatLiable = string.Equals(dto.VatLiable, "vLiable", StringComparison.OrdinalIgnoreCase)
                            ? BoVatStatus.vLiable
                            : BoVatStatus.vExempted;
                    }

                    if (!string.IsNullOrWhiteSpace(dto.HouseBank))
                        oBusinessPartner.HouseBank = dto.HouseBank;

                    if (!string.IsNullOrWhiteSpace(dto.HouseBankCountry))
                        oBusinessPartner.HouseBankCountry = dto.HouseBankCountry;

                    if (!string.IsNullOrWhiteSpace(dto.HouseBankAccount))
                        oBusinessPartner.HouseBankAccount = dto.HouseBankAccount;

                    // Propiedades (Properties1 - Properties64)
                    foreach (var udf in dto.UserFields)
                    {
                        if (udf.Key.StartsWith("Properties", StringComparison.OrdinalIgnoreCase) &&
                            int.TryParse(udf.Key.Substring(10), out int propIndex) && propIndex >= 1 && propIndex <= 64)
                        {
                            var propVal = udf.Value?.ToString()?.ToUpperInvariant();
                            oBusinessPartner.Properties[propIndex] = (propVal == "TYES" || propVal == "TRUE" || propVal == "1")
                                ? BoYesNoEnum.tYES
                                : BoYesNoEnum.tNO;
                        }
                    }

                    // Retenciones
                    if (!string.IsNullOrWhiteSpace(dto.SubjectToWithholdingTax))
                    {
                        bool isWithholding = string.Equals(dto.SubjectToWithholdingTax, "boYES", StringComparison.OrdinalIgnoreCase) ||
                                             string.Equals(dto.SubjectToWithholdingTax, "tYES", StringComparison.OrdinalIgnoreCase);
                        oBusinessPartner.SubjectToWithholdingTax = isWithholding
                            ? (BoYesNoNoneEnum)BoYesNoEnum.tYES
                            : (BoYesNoNoneEnum)BoYesNoEnum.tNO;
                    }

                    // Campos de Usuario (UDF) dinámicos
                    foreach (var kvp in dto.UserFields)
                    {
                        if (kvp.Key.StartsWith("Properties", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var fieldName = kvp.Key.StartsWith("U_") ? kvp.Key : $"U_{kvp.Key}";
                        try
                        {
                            if (kvp.Value != null && kvp.Value.ToString() != "NULL")
                            {
                                oBusinessPartner.UserFields.Fields.Item(fieldName).Value = kvp.Value.ToString();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug("Aviso UDF '{Field}': {Message}", fieldName, ex.Message);
                        }
                    }

                    // Sincronizar Direcciones
                    if (dto.BPAddresses != null && dto.BPAddresses.Count > 0)
                    {
                        foreach (var dir in dto.BPAddresses)
                        {
                            bool addressExists = false;
                            for (int i = 0; i < oBusinessPartner.Addresses.Count; i++)
                            {
                                oBusinessPartner.Addresses.SetCurrentLine(i);
                                if (string.Equals(oBusinessPartner.Addresses.AddressName, dir.AddressName, StringComparison.OrdinalIgnoreCase))
                                {
                                    addressExists = true;
                                    break;
                                }
                            }

                            if (!addressExists)
                            {
                                if (oBusinessPartner.Addresses.Count > 0 && !string.IsNullOrEmpty(oBusinessPartner.Addresses.AddressName))
                                {
                                    oBusinessPartner.Addresses.Add();
                                }
                                oBusinessPartner.Addresses.SetCurrentLine(oBusinessPartner.Addresses.Count - 1);
                            }

                            if (!string.IsNullOrWhiteSpace(dir.AddressName))
                                oBusinessPartner.Addresses.AddressName = dir.AddressName;

                            if (!string.IsNullOrWhiteSpace(dir.Street))
                                oBusinessPartner.Addresses.Street = dir.Street;

                            if (!string.IsNullOrWhiteSpace(dir.City))
                                oBusinessPartner.Addresses.City = dir.City;

                            if (!string.IsNullOrWhiteSpace(dir.County))
                                oBusinessPartner.Addresses.County = dir.County;

                            if (!string.IsNullOrWhiteSpace(dir.State))
                                oBusinessPartner.Addresses.State = dir.State;

                            if (!string.IsNullOrWhiteSpace(dir.Country))
                                oBusinessPartner.Addresses.Country = dir.Country;
                        }
                    }

                    // Sincronizar Contactos
                    if (dto.ContactEmployees != null && dto.ContactEmployees.Count > 0)
                    {
                        foreach (var c in dto.ContactEmployees)
                        {
                            bool contactExists = false;
                            for (int i = 0; i < oBusinessPartner.ContactEmployees.Count; i++)
                            {
                                oBusinessPartner.ContactEmployees.SetCurrentLine(i);
                                if ((c.InternalCode > 0 && oBusinessPartner.ContactEmployees.InternalCode == c.InternalCode) ||
                                    string.Equals(oBusinessPartner.ContactEmployees.Name, c.Name, StringComparison.OrdinalIgnoreCase))
                                {
                                    contactExists = true;
                                    break;
                                }
                            }

                            if (!contactExists)
                            {
                                if (oBusinessPartner.ContactEmployees.Count > 0 && !string.IsNullOrEmpty(oBusinessPartner.ContactEmployees.Name))
                                {
                                    oBusinessPartner.ContactEmployees.Add();
                                }
                                oBusinessPartner.ContactEmployees.SetCurrentLine(oBusinessPartner.ContactEmployees.Count - 1);
                            }

                            if (!string.IsNullOrWhiteSpace(c.Name))
                                oBusinessPartner.ContactEmployees.Name = c.Name;

                            if (!string.IsNullOrWhiteSpace(c.FirstName))
                                oBusinessPartner.ContactEmployees.FirstName = c.FirstName;

                            if (!string.IsNullOrWhiteSpace(c.LastName))
                                oBusinessPartner.ContactEmployees.LastName = c.LastName;

                            if (!string.IsNullOrWhiteSpace(c.Position))
                                oBusinessPartner.ContactEmployees.Position = c.Position;

                            if (!string.IsNullOrWhiteSpace(c.Phone1))
                                oBusinessPartner.ContactEmployees.Phone1 = c.Phone1;

                            if (!string.IsNullOrWhiteSpace(c.EmailAddress))
                                oBusinessPartner.ContactEmployees.E_Mail = c.EmailAddress;

                            foreach (var cudf in c.UserFields)
                            {
                                var fname = cudf.Key.StartsWith("U_") ? cudf.Key : $"U_{cudf.Key}";
                                try { if (cudf.Value != null) oBusinessPartner.ContactEmployees.UserFields.Fields.Item(fname).Value = cudf.Value.ToString(); } catch { }
                            }
                        }
                    }

                    // Sincronizar Cuentas Bancarias
                    if (dto.BPBankAccounts != null && dto.BPBankAccounts.Count > 0)
                    {
                        foreach (var bAcc in dto.BPBankAccounts)
                        {
                            bool accExists = false;
                            for (int i = 0; i < oBusinessPartner.BPBankAccounts.Count; i++)
                            {
                                oBusinessPartner.BPBankAccounts.SetCurrentLine(i);
                                if (string.Equals(oBusinessPartner.BPBankAccounts.AccountNo, bAcc.AccountNumber, StringComparison.OrdinalIgnoreCase))
                                {
                                    accExists = true;
                                    break;
                                }
                            }

                            if (!accExists)
                            {
                                if (oBusinessPartner.BPBankAccounts.Count > 0 && !string.IsNullOrEmpty(oBusinessPartner.BPBankAccounts.AccountNo))
                                {
                                    oBusinessPartner.BPBankAccounts.Add();
                                }
                                oBusinessPartner.BPBankAccounts.SetCurrentLine(oBusinessPartner.BPBankAccounts.Count - 1);
                            }

                            if (!string.IsNullOrWhiteSpace(bAcc.AccountNumber))
                                oBusinessPartner.BPBankAccounts.AccountNo = bAcc.AccountNumber;

                            if (!string.IsNullOrWhiteSpace(bAcc.BankCode))
                                oBusinessPartner.BPBankAccounts.BankCode = bAcc.BankCode;

                            if (!string.IsNullOrWhiteSpace(bAcc.AccountName))
                                oBusinessPartner.BPBankAccounts.AccountName = bAcc.AccountName;

                            if (!string.IsNullOrWhiteSpace(bAcc.Country))
                                oBusinessPartner.BPBankAccounts.Country = bAcc.Country;

                            if (!string.IsNullOrWhiteSpace(bAcc.UserNo1))
                                try { oBusinessPartner.BPBankAccounts.UserFields.Fields.Item("UsrNumber1").Value = bAcc.UserNo1; } catch { }
                            if (!string.IsNullOrWhiteSpace(bAcc.UserNo2))
                                try { oBusinessPartner.BPBankAccounts.UserFields.Fields.Item("UsrNumber2").Value = bAcc.UserNo2; } catch { }
                            if (!string.IsNullOrWhiteSpace(bAcc.UserNo3))
                                try { oBusinessPartner.BPBankAccounts.UserFields.Fields.Item("UsrNumber3").Value = bAcc.UserNo3; } catch { }
                        }
                    }

                    // Anexo si aplica
                    if (dto.AttachmentEntry.HasValue && dto.AttachmentEntry.Value > 0)
                    {
                        oBusinessPartner.AttachmentEntry = dto.AttachmentEntry.Value;
                    }

                    // Ejecutar Update en SAP
                    int updateResult = oBusinessPartner.Update();

                    if (updateResult == 0)
                    {
                        _logger.LogInformation("Socio de negocio '{CardCode}' actualizado exitosamente en SAP.", cardCode);
                        return Task.FromResult((true, cardCode, (string?)null));
                    }
                    else
                    {
                        company.GetLastError(out int lastErrorCode, out string lastErrorDescription);
                        _logger.LogError("Fallo al actualizar socio de negocio en SAP ({Code}): {Error}", lastErrorCode, lastErrorDescription);
                        return Task.FromResult((false, cardCode, (string?)$"Error SAP ({lastErrorCode}): {lastErrorDescription}"));
                    }
                }
                finally
                {
                    ComHelper.Release(oBusinessPartner);
                }
            });
        }

        public async Task<(bool Success, int AttachmentEntry, string? ErrorMessage)> CreateOrUpdateAttachmentAsync(UserSession session, AttachmentDto dto)
        {
            var connInfo = BuildConnectionInfo(session.CompanyDB, session.UserName, session.Password);

            bool isUpdate = dto.AbsoluteEntry.HasValue && dto.AbsoluteEntry.Value > 0;
            _logger.LogInformation(
                "DI API: {Action} Anexo (Attachments2) en SAP | DB: {DB} | Usuario SAP: {User}",
                isUpdate ? $"Actualizando anexo #{dto.AbsoluteEntry}" : "Creando nuevo anexo", session.CompanyDB, session.UserName);

            return await _companyPool.ExecuteAsync(connInfo, company =>
            {
                Attachments2? oAttachment = null;
                try
                {
                    oAttachment = (Attachments2)company.GetBusinessObject(BoObjectTypes.oAttachments2);

                    if (isUpdate)
                    {
                        if (!oAttachment.GetByKey(dto.AbsoluteEntry!.Value))
                        {
                            return Task.FromResult((false, 0, (string?)$"No se pudo encontrar el anexo con ID {dto.AbsoluteEntry.Value} en SAP."));
                        }
                    }
                    else
                    {
                        oAttachment.Lines.Add();
                    }

                    int newLinesAdded = 0;
                    foreach (var linea in dto.Lines)
                    {
                        bool lineExists = false;
                        int targetLineIndex = -1;

                        if (isUpdate)
                        {
                            for (int i = 0; i < oAttachment.Lines.Count; i++)
                            {
                                oAttachment.Lines.SetCurrentLine(i);
                                string nombreSinExt = System.IO.Path.GetFileNameWithoutExtension(linea.FileName);

                                if (string.Equals(oAttachment.Lines.FileName, nombreSinExt, StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(oAttachment.Lines.FileName, linea.FileName, StringComparison.OrdinalIgnoreCase))
                                {
                                    lineExists = true;
                                    targetLineIndex = i;
                                    break;
                                }
                            }
                        }

                        if (lineExists)
                        {
                            oAttachment.Lines.SetCurrentLine(targetLineIndex);
                        }
                        else
                        {
                            if (isUpdate || newLinesAdded > 0)
                            {
                                oAttachment.Lines.Add();
                            }
                            newLinesAdded++;
                        }

                        if (!string.IsNullOrWhiteSpace(linea.SourcePath))
                            oAttachment.Lines.SourcePath = linea.SourcePath;

                        if (!string.IsNullOrWhiteSpace(linea.FileName))
                            oAttachment.Lines.FileName = System.IO.Path.GetFileNameWithoutExtension(linea.FileName);

                        if (!string.IsNullOrWhiteSpace(linea.FileExtension))
                            oAttachment.Lines.FileExtension = linea.FileExtension.TrimStart('.');
                        else if (!string.IsNullOrWhiteSpace(linea.FileName))
                            oAttachment.Lines.FileExtension = System.IO.Path.GetExtension(linea.FileName).TrimStart('.');

                        oAttachment.Lines.Override = BoYesNoEnum.tYES;

                        if (!string.IsNullOrWhiteSpace(linea.FreeText))
                            oAttachment.Lines.FreeText = linea.FreeText;

                        // UDFs de Anexos
                        if (!string.IsNullOrWhiteSpace(linea.U_TipoDoc))
                            try { oAttachment.Lines.UserFields.Fields.Item("U_TipoDoc").Value = linea.U_TipoDoc; } catch { }
                        if (!string.IsNullOrWhiteSpace(linea.U_PCV))
                            try { oAttachment.Lines.UserFields.Fields.Item("U_PCV").Value = linea.U_PCV; } catch { }
                        if (!string.IsNullOrWhiteSpace(linea.U_Inmueble))
                            try { oAttachment.Lines.UserFields.Fields.Item("U_Inmueble").Value = linea.U_Inmueble; } catch { }
                        if (!string.IsNullOrWhiteSpace(linea.U_Docs))
                            try { oAttachment.Lines.UserFields.Fields.Item("U_Docs").Value = linea.U_Docs; } catch { }

                        if (string.Equals(linea.CopyToTargetDoc, "tYES", StringComparison.OrdinalIgnoreCase))
                        {
                            oAttachment.Lines.CopyToTargetDoc = BoYesNoEnum.tYES;
                        }

                        foreach (var kvp in linea.UserFields)
                        {
                            var fname = kvp.Key.StartsWith("U_") ? kvp.Key : $"U_{kvp.Key}";
                            try { if (kvp.Value != null) oAttachment.Lines.UserFields.Fields.Item(fname).Value = kvp.Value.ToString(); } catch { }
                        }
                    }

                    int opResult = isUpdate ? oAttachment.Update() : oAttachment.Add();

                    if (opResult == 0)
                    {
                        int entry = isUpdate ? dto.AbsoluteEntry!.Value : int.Parse(company.GetNewObjectKey());
                        _logger.LogInformation("Anexo #{Entry} guardado exitosamente en SAP.", entry);
                        return Task.FromResult((true, entry, (string?)null));
                    }
                    else
                    {
                        company.GetLastError(out int lastErrorCode, out string lastErrorDescription);
                        _logger.LogError("Fallo al guardar anexo en SAP ({Code}): {Error}", lastErrorCode, lastErrorDescription);
                        return Task.FromResult((false, 0, (string?)$"Error SAP ({lastErrorCode}): {lastErrorDescription}"));
                    }
                }
                finally
                {
                    ComHelper.Release(oAttachment);
                }
            });
        }

        #region ApprovalRequests (Gestión de Autorizaciones)

        public async Task<ApprovalRequestDto?> GetApprovalRequestAsync(UserSession session, int code)
        {
            var connInfo = BuildConnectionInfo(session.CompanyDB, session.UserName, session.Password);

            _logger.LogInformation("DI API: Consultando Solicitud de Aprobación #{Code} en SAP | DB: {DB} | Usuario: {User}",
                code, session.CompanyDB, session.UserName);

            return await _companyPool.ExecuteAsync(connInfo, company =>
            {
                CompanyService? compService = null;
                ApprovalRequestsService? appService = null;
                ApprovalRequestParams? appParams = null;
                ApprovalRequest? appReq = null;

                try
                {
                    compService = company.GetCompanyService();
                    appService = (ApprovalRequestsService)compService.GetBusinessService(ServiceTypes.ApprovalRequestsService);
                    appParams = (ApprovalRequestParams)appService.GetDataInterface(ApprovalRequestsServiceDataInterfaces.arsApprovalRequestParams);
                    appParams.Code = code;

                    appReq = appService.GetApprovalRequest(appParams);
                    if (appReq == null)
                    {
                        return Task.FromResult<ApprovalRequestDto?>(null);
                    }

                    var appDto = new ApprovalRequestDto
                    {
                        Code = appReq.Code,
                        ApprovalTemplatesID = appReq.ApprovalTemplatesID > 0 ? appReq.ApprovalTemplatesID : null,
                        ObjectType = appReq.ObjectType,
                        IsDraft = appReq.IsDraft,
                        ObjectEntry = appReq.ObjectEntry > 0 ? appReq.ObjectEntry : null,
                        Status = appReq.Status.ToString(),
                        Remarks = appReq.Remarks,
                        CurrentStage = appReq.CurrentStage > 0 ? appReq.CurrentStage : null,
                        OriginatorID = appReq.OriginatorID > 0 ? appReq.OriginatorID : null,
                        CreationDate = CleanSapDate(appReq.CreationDate)?.ToString("yyyy-MM-dd"),
                        CreationTime = FormatSapTime(appReq.CreationTime),
                        DraftEntry = appReq.DraftEntry > 0 ? appReq.DraftEntry : null,
                        DraftType = appReq.DraftType
                    };

                    // Obtener líneas de etapas y autorizadores asignados
                    ApprovalRequestLines? lines = null;
                    try
                    {
                        lines = appReq.ApprovalRequestLines;
                        if (lines != null)
                        {
                            for (int i = 0; i < lines.Count; i++)
                            {
                                ApprovalRequestLine? line = null;
                                try
                                {
                                    line = lines.Item(i);
                                    if (line != null)
                                    {
                                        appDto.ApprovalRequestLines.Add(new ApprovalRequestLineDto
                                        {
                                            StageCode = line.StageCode,
                                            UserID = line.UserID,
                                            Status = line.Status.ToString(),
                                            Remarks = line.Remarks,
                                            UpdateDate = CleanSapDate(line.UpdateDate)?.ToString("yyyy-MM-dd"),
                                            UpdateTime = FormatSapTime(line.UpdateTime),
                                            CreationDate = CleanSapDate(line.CreationDate)?.ToString("yyyy-MM-dd"),
                                            CreationTime = FormatSapTime(line.CreationTime)
                                        });
                                    }
                                }
                                finally
                                {
                                    ComHelper.Release(line);
                                }
                            }
                        }
                    }
                    finally
                    {
                        ComHelper.Release(lines);
                    }

                    // Obtener historial de decisiones
                    ApprovalRequestDecisions? decisions = null;
                    try
                    {
                        decisions = appReq.ApprovalRequestDecisions;
                        if (decisions != null)
                        {
                            for (int i = 0; i < decisions.Count; i++)
                            {
                                ApprovalRequestDecision? dec = null;
                                try
                                {
                                    dec = decisions.Item(i);
                                    if (dec != null)
                                    {
                                        appDto.ApprovalRequestDecisions.Add(new ApprovalRequestDecisionDto
                                        {
                                            ApproverUserName = dec.ApproverUserName,
                                            Status = dec.Status.ToString(),
                                            Remarks = dec.Remarks
                                        });
                                    }
                                }
                                finally
                                {
                                    ComHelper.Release(dec);
                                }
                            }
                        }
                    }
                    finally
                    {
                        ComHelper.Release(decisions);
                    }

                    return Task.FromResult<ApprovalRequestDto?>(appDto);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("No se encontró o falló la consulta de la solicitud #{Code} en SAP: {Error}", code, ex.Message);
                    return Task.FromResult<ApprovalRequestDto?>(null);
                }
                finally
                {
                    ComHelper.Release(appReq);
                    ComHelper.Release(appParams);
                    ComHelper.Release(appService);
                    ComHelper.Release(compService);
                }
            });
        }

        public async Task<IEnumerable<ApprovalRequestDto>> GetApprovalRequestsFilteredAsync(UserSession session, ApprovalRequestFilterDto filter)
        {
            var connInfo = BuildConnectionInfo(session.CompanyDB, session.UserName, session.Password);

            return await _companyPool.ExecuteAsync(connInfo, company =>
            {
                Recordset? oRs = null;
                try
                {
                    oRs = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);
                    var whereClauses = new List<string>();

                    if (filter.OriginatorID.HasValue && filter.OriginatorID.Value > 0)
                        whereClauses.Add($"T0.\"OwnerID\" = {filter.OriginatorID.Value}");

                    if (filter.DraftEntry.HasValue && filter.DraftEntry.Value > 0)
                        whereClauses.Add($"T0.\"DraftEntry\" = {filter.DraftEntry.Value}");

                    if (!string.IsNullOrWhiteSpace(filter.ObjectType))
                        whereClauses.Add($"T0.\"ObjType\" = '{filter.ObjectType.Trim()}'");

                    if (!string.IsNullOrWhiteSpace(filter.Status))
                    {
                        var statusChar = filter.Status.ToUpperInvariant() switch
                        {
                            "ARSAPPROVED" or "APPROVED" or "Y" => "Y",
                            "ARSNOTAPPROVED" or "NOTAPPROVED" or "REJECTED" or "N" => "N",
                            "ARSCANCELED" or "CANCELED" or "C" => "C",
                            "ARSGENERATED" or "GENERATED" or "A" => "A",
                            _ => "W"
                        };
                        whereClauses.Add($"T0.\"Status\" = '{statusChar}'");
                    }

                    if (filter.FromDate.HasValue)
                        whereClauses.Add($"T0.\"DocDate\" >= '{filter.FromDate.Value:yyyy-MM-dd}'");

                    if (filter.ToDate.HasValue)
                        whereClauses.Add($"T0.\"DocDate\" <= '{filter.ToDate.Value:yyyy-MM-dd}'");

                    string sql;
                    if (filter.UserID.HasValue && filter.UserID.Value > 0)
                    {
                        whereClauses.Add($"T1.\"UserID\" = {filter.UserID.Value}");
                        string whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";
                        sql = $"SELECT DISTINCT TOP 100 T0.\"WddCode\", T0.\"WtmCode\", T0.\"ObjType\", T0.\"DocEntry\", T0.\"Status\", T0.\"Remarks\", T0.\"CurrStep\", T0.\"OwnerID\", T0.\"DocDate\", T0.\"DocTime\", T0.\"DraftEntry\", T0.\"DraftType\", T0.\"IsDraft\" FROM \"OWDD\" T0 INNER JOIN \"WDD1\" T1 ON T0.\"WddCode\" = T1.\"WddCode\" {whereSql} ORDER BY T0.\"WddCode\" DESC";
                    }
                    else
                    {
                        string whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";
                        sql = $"SELECT TOP 100 T0.\"WddCode\", T0.\"WtmCode\", T0.\"ObjType\", T0.\"DocEntry\", T0.\"Status\", T0.\"Remarks\", T0.\"CurrStep\", T0.\"OwnerID\", T0.\"DocDate\", T0.\"DocTime\", T0.\"DraftEntry\", T0.\"DraftType\", T0.\"IsDraft\" FROM \"OWDD\" T0 {whereSql} ORDER BY T0.\"WddCode\" DESC";
                    }

                    oRs.DoQuery(sql);
                    var list = new List<ApprovalRequestDto>();

                    while (!oRs.EoF)
                    {
                        list.Add(new ApprovalRequestDto
                        {
                            Code = Convert.ToInt32(oRs.Fields.Item("WddCode").Value),
                            ApprovalTemplatesID = oRs.Fields.Item("WtmCode").Value is DBNull ? null : Convert.ToInt32(oRs.Fields.Item("WtmCode").Value),
                            ObjectType = oRs.Fields.Item("ObjType").Value?.ToString()?.Trim(),
                            IsDraft = oRs.Fields.Item("IsDraft").Value?.ToString()?.Trim() ?? "Y",
                            ObjectEntry = oRs.Fields.Item("DocEntry").Value is DBNull ? null : (Convert.ToInt32(oRs.Fields.Item("DocEntry").Value) == 0 ? null : Convert.ToInt32(oRs.Fields.Item("DocEntry").Value)),
                            Status = MapApprovalStatus(oRs.Fields.Item("Status").Value?.ToString()?.Trim()),
                            Remarks = oRs.Fields.Item("Remarks").Value?.ToString(),
                            CurrentStage = oRs.Fields.Item("CurrStep").Value is DBNull ? null : Convert.ToInt32(oRs.Fields.Item("CurrStep").Value),
                            OriginatorID = oRs.Fields.Item("OwnerID").Value is DBNull ? null : Convert.ToInt32(oRs.Fields.Item("OwnerID").Value),
                            CreationDate = FormatSapDate(oRs.Fields.Item("DocDate").Value),
                            CreationTime = FormatSapTime(oRs.Fields.Item("DocTime").Value),
                            DraftEntry = oRs.Fields.Item("DraftEntry").Value is DBNull ? null : Convert.ToInt32(oRs.Fields.Item("DraftEntry").Value),
                            DraftType = oRs.Fields.Item("DraftType").Value?.ToString()?.Trim()
                        });
                        oRs.MoveNext();
                    }

                    return Task.FromResult<IEnumerable<ApprovalRequestDto>>(list);
                }
                finally
                {
                    ComHelper.Release(oRs);
                }
            });
        }

        public async Task<(bool Success, int Code, string? ErrorMessage)> UpdateApprovalRequestAsync(UserSession session, int code, UpdateApprovalRequestDto dto)
        {
            var connInfo = BuildConnectionInfo(session.CompanyDB, session.UserName, session.Password);

            _logger.LogInformation("DI API: Actualizando Solicitud de Aprobación #{Code} en SAP | DB: {DB} | Usuario: {User} | Operador: {AuditUser}",
                code, session.CompanyDB, session.UserName, session.AuditUser ?? "N/A");

            return await _companyPool.ExecuteAsync(connInfo, company =>
            {
                CompanyService? compService = null;
                ApprovalRequestsService? appService = null;
                ApprovalRequestParams? appParams = null;
                ApprovalRequest? appReq = null;

                try
                {
                    compService = company.GetCompanyService();
                    appService = (ApprovalRequestsService)compService.GetBusinessService(ServiceTypes.ApprovalRequestsService);
                    appParams = (ApprovalRequestParams)appService.GetDataInterface(ApprovalRequestsServiceDataInterfaces.arsApprovalRequestParams);
                    appParams.Code = code;

                    appReq = appService.GetApprovalRequest(appParams);

                    // Registrar Decisiones de Autorización si vienen en el payload
                    if (dto.ApprovalRequestDecisions != null && dto.ApprovalRequestDecisions.Count > 0)
                    {
                        var decisions = appReq.ApprovalRequestDecisions;
                        foreach (var dec in dto.ApprovalRequestDecisions)
                        {
                            var decisionItem = decisions.Add();
                            if (!string.IsNullOrWhiteSpace(dec.ApproverUserName))
                                decisionItem.ApproverUserName = dec.ApproverUserName;
                            else if (!string.IsNullOrWhiteSpace(session.UserName))
                                decisionItem.ApproverUserName = session.UserName;

                            if (!string.IsNullOrWhiteSpace(dec.ApproverPassword))
                                decisionItem.ApproverPassword = dec.ApproverPassword;
                            else if (!string.IsNullOrWhiteSpace(session.Password))
                                decisionItem.ApproverPassword = session.Password;

                            if (!string.IsNullOrWhiteSpace(dec.Remarks))
                                decisionItem.Remarks = dec.Remarks;

                            var statusDec = dec.Status?.ToUpperInvariant();
                            decisionItem.Status = (statusDec == "Y" || statusDec == "ARDAPPROVED" || statusDec == "APPROVED")
                                ? BoApprovalRequestDecisionEnum.ardApproved
                                : (statusDec == "N" || statusDec == "ARDNOTAPPROVED" || statusDec == "NOTAPPROVED" || statusDec == "REJECTED")
                                    ? BoApprovalRequestDecisionEnum.ardNotApproved
                                    : BoApprovalRequestDecisionEnum.ardPending;
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(dto.Status))
                    {
                        var decisionItem = appReq.ApprovalRequestDecisions.Add();
                        decisionItem.ApproverUserName = session.UserName;
                        decisionItem.ApproverPassword = session.Password;
                        if (!string.IsNullOrWhiteSpace(dto.Remarks))
                            decisionItem.Remarks = dto.Remarks;

                        var statusDec = dto.Status.ToUpperInvariant();
                        decisionItem.Status = (statusDec == "Y" || statusDec == "ARSAPPROVED" || statusDec == "APPROVED" || statusDec == "ARDAPPROVED")
                            ? BoApprovalRequestDecisionEnum.ardApproved
                            : (statusDec == "N" || statusDec == "ARSNOTAPPROVED" || statusDec == "NOTAPPROVED" || statusDec == "REJECTED" || statusDec == "ARDNOTAPPROVED")
                                ? BoApprovalRequestDecisionEnum.ardNotApproved
                                : BoApprovalRequestDecisionEnum.ardPending;
                    }

                    appService.UpdateRequest(appReq);
                    _logger.LogInformation("Solicitud de aprobación #{Code} actualizada con éxito en SAP.", code);
                    return Task.FromResult((true, code, (string?)null));
                }
                catch (Exception ex)
                {
                    company.GetLastError(out int lastErrorCode, out string lastErrorDescription);
                    string errorMsg = !string.IsNullOrEmpty(lastErrorDescription) ? $"Error SAP ({lastErrorCode}): {lastErrorDescription}" : ex.Message;
                    _logger.LogError(ex, "Fallo al actualizar solicitud de aprobación #{Code} en SAP: {Error}", code, errorMsg);
                    return Task.FromResult((false, code, (string?)errorMsg));
                }
                finally
                {
                    ComHelper.Release(appReq);
                    ComHelper.Release(appParams);
                    ComHelper.Release(appService);
                    ComHelper.Release(compService);
                }
            });
        }

        #endregion

        #region Drafts (Gestión de Documentos Preliminares / Borradores)

        public async Task<DraftDto?> GetDraftAsync(UserSession session, int docEntry)
        {
            var connInfo = BuildConnectionInfo(session.CompanyDB, session.UserName, session.Password);

            _logger.LogInformation("DI API: Consultando Borrador #{DocEntry} en SAP | DB: {DB} | Usuario: {User}",
                docEntry, session.CompanyDB, session.UserName);

            return await _companyPool.ExecuteAsync(connInfo, company =>
            {
                Documents? oDraft = null;
                try
                {
                    oDraft = (Documents)company.GetBusinessObject(BoObjectTypes.oDrafts);

                    if (!oDraft.GetByKey(docEntry))
                    {
                        return Task.FromResult<DraftDto?>(null);
                    }

                    var draftDto = new DraftDto
                    {
                        DocEntry = oDraft.DocEntry,
                        DocNum = oDraft.DocNum,
                        DocType = oDraft.DocType == BoDocumentTypes.dDocument_Items ? "dDocument_Items" : "dDocument_Service",
                        DocObjectCode = oDraft.DocObjectCodeEx.ToString(),
                        DocDate = FormatSapDate(oDraft.DocDate),
                        DocDueDate = FormatSapDate(oDraft.DocDueDate),
                        TaxDate = FormatSapDate(oDraft.TaxDate),
                        CardCode = oDraft.CardCode,
                        CardName = oDraft.CardName,
                        Address = oDraft.Address,
                        Address2 = oDraft.Address2,
                        NumAtCard = oDraft.NumAtCard,
                        DocTotal = (decimal)oDraft.DocTotal,
                        DocTotalSys = (decimal)oDraft.DocTotalSys,
                        DocTotalFc = (decimal)oDraft.DocTotalFc,
                        DocCurrency = oDraft.DocCurrency,
                        DocRate = (decimal)oDraft.DocRate,
                        VatSum = (decimal)oDraft.VatSum,
                        DiscountPercent = (decimal)oDraft.DiscountPercent,
                        Comments = oDraft.Comments,
                        JournalMemo = oDraft.JournalMemo,
                        PaymentGroupCode = oDraft.PaymentGroupCode,
                        Series = oDraft.Series,
                        AttachmentEntry = oDraft.AttachmentEntry > 0 ? oDraft.AttachmentEntry : null,
                        DocumentStatus = oDraft.DocumentStatus == BoStatus.bost_Open ? "bost_Open" : "bost_Close",
                        AuthorizationStatus = oDraft.AuthorizationStatus switch
                        {
                            DocumentAuthorizationStatusEnum.dasApproved => "dasApproved",
                            DocumentAuthorizationStatusEnum.dasPending => "dasPending",
                            DocumentAuthorizationStatusEnum.dasRejected => "dasRejected",
                            DocumentAuthorizationStatusEnum.dasGenerated => "dasGenerated",
                            DocumentAuthorizationStatusEnum.dasGeneratedbyAuthorizer => "dasGeneratedbyAuthorizer",
                            DocumentAuthorizationStatusEnum.dasCancelled => "dasCancelled",
                            _ => "dasWithout"
                        },
                        Confirmed = oDraft.Confirmed == BoYesNoEnum.tYES ? "tYES" : "tNO",
                        UserSign = oDraft.UserSign > 0 ? oDraft.UserSign : null,
                        FederalTaxID = oDraft.FederalTaxID,
                        CreationDate = FormatSapDate(oDraft.CreationDate)
                    };

                    // Mapear UDFs de Cabecera
                    try
                    {
                        var userFields = oDraft.UserFields;
                        var fields = userFields?.Fields;
                        if (fields != null)
                        {
                            for (int i = 0; i < fields.Count; i++)
                            {
                                Field? f = null;
                                try
                                {
                                    f = fields.Item(i);
                                    if (f != null)
                                    {
                                        var fname = f.Name.StartsWith("U_") ? f.Name : $"U_{f.Name}";
                                        draftDto.UserFields[fname] = f.Value is DateTime dt ? FormatSapDate(dt) : f.Value;
                                    }
                                }
                                finally
                                {
                                    ComHelper.Release(f);
                                }
                            }
                            ComHelper.Release(fields);
                        }
                        ComHelper.Release(userFields);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug("Aviso UDFs Borrador: {Message}", ex.Message);
                    }

                    // Mapear Líneas de Detalle (DocumentLines)
                    var lines = oDraft.Lines;
                    try
                    {
                        if (lines != null)
                        {
                            for (int i = 0; i < lines.Count; i++)
                            {
                                lines.SetCurrentLine(i);
                                var lineDto = new DraftDocumentLineDto
                                {
                                    LineNum = lines.LineNum,
                                    ItemCode = lines.ItemCode,
                                    ItemDescription = lines.ItemDescription,
                                    Quantity = (decimal)lines.Quantity,
                                    Price = (decimal)lines.Price,
                                    PriceAfterVAT = (decimal)lines.PriceAfterVAT,
                                    Currency = lines.Currency,
                                    DiscountPercent = (decimal)lines.DiscountPercent,
                                    WarehouseCode = lines.WarehouseCode,
                                    AccountCode = lines.AccountCode,
                                    CostingCode = lines.CostingCode,
                                    CostingCode2 = lines.CostingCode2,
                                    CostingCode3 = lines.CostingCode3,
                                    CostingCode4 = lines.CostingCode4,
                                    CostingCode5 = lines.CostingCode5,
                                    ProjectCode = lines.ProjectCode,
                                    TaxCode = lines.TaxCode,
                                    VatGroup = lines.VatGroup,
                                    LineTotal = (decimal)lines.LineTotal,
                                    GrossTotal = (decimal)lines.GrossTotal,
                                    GrossTotalSC = (decimal)lines.GrossTotalSC,
                                    TaxTotal = (decimal)lines.TaxTotal,
                                    TaxPercentagePerRow = (decimal)lines.TaxPercentagePerRow,
                                    MeasureUnit = lines.MeasureUnit,
                                    UoMCode = lines.UoMCode,
                                    FreeText = lines.FreeText,
                                    LineStatus = lines.LineStatus == BoStatus.bost_Open ? "bost_Open" : "bost_Close"
                                };

                                // UDFs a nivel de Línea
                                try
                                {
                                    var lineUserFields = lines.UserFields;
                                    var lfields = lineUserFields?.Fields;
                                    if (lfields != null)
                                    {
                                        for (int u = 0; u < lfields.Count; u++)
                                        {
                                            Field? lf = null;
                                            try
                                            {
                                                lf = lfields.Item(u);
                                                if (lf != null)
                                                {
                                                    var lfname = lf.Name.StartsWith("U_") ? lf.Name : $"U_{lf.Name}";
                                                    lineDto.UserFields[lfname] = lf.Value is DateTime ldt ? FormatSapDate(ldt) : lf.Value;
                                                }
                                            }
                                            finally
                                            {
                                                ComHelper.Release(lf);
                                            }
                                        }
                                        ComHelper.Release(lfields);
                                    }
                                    ComHelper.Release(lineUserFields);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogDebug("Aviso UDFs Línea Borrador: {Message}", ex.Message);
                                }

                                draftDto.DocumentLines.Add(lineDto);
                            }
                        }
                    }
                    finally
                    {
                        ComHelper.Release(lines);
                    }

                    // Mapear AddressExtension
                    try
                    {
                        var addrExt = oDraft.AddressExtension;
                        if (addrExt != null)
                        {
                            draftDto.AddressExtension = new DraftAddressExtensionDto
                            {
                                DocEntry = draftDto.DocEntry,
                                ShipToStreet = addrExt.ShipToStreet,
                                ShipToCity = addrExt.ShipToCity,
                                ShipToState = addrExt.ShipToState,
                                ShipToCountry = addrExt.ShipToCountry,
                                BillToStreet = addrExt.BillToStreet,
                                BillToCity = addrExt.BillToCity,
                                BillToState = addrExt.BillToState,
                                BillToCountry = addrExt.BillToCountry
                            };
                            ComHelper.Release(addrExt);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug("Aviso AddressExtension Borrador: {Message}", ex.Message);
                    }

                    // Mapear TaxExtension
                    try
                    {
                        var taxExt = oDraft.TaxExtension;
                        if (taxExt != null)
                        {
                            draftDto.TaxExtension = new DraftTaxExtensionDto
                            {
                                DocEntry = draftDto.DocEntry,
                                TaxId0 = taxExt.TaxId0,
                                StreetS = taxExt.StreetS,
                                CityS = taxExt.CityS,
                                StateS = taxExt.StateS,
                                CountryS = taxExt.CountryS,
                                MainUsage = taxExt.MainUsage.ToString()
                            };
                            ComHelper.Release(taxExt);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug("Aviso TaxExtension Borrador: {Message}", ex.Message);
                    }

                    return Task.FromResult<DraftDto?>(draftDto);
                }
                finally
                {
                    ComHelper.Release(oDraft);
                }
            });
        }

        public async Task<IEnumerable<DraftDto>> GetDraftsFilteredAsync(UserSession session, DraftFilterDto filter)
        {
            var connInfo = BuildConnectionInfo(session.CompanyDB, session.UserName, session.Password);

            return await _companyPool.ExecuteAsync(connInfo, company =>
            {
                Recordset? oRs = null;
                try
                {
                    oRs = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);
                    var whereClauses = new List<string>();

                    if (!string.IsNullOrWhiteSpace(filter.DocObjectCode))
                        whereClauses.Add($"\"ObjType\" = '{filter.DocObjectCode.Trim()}'");

                    if (!string.IsNullOrWhiteSpace(filter.CardCode))
                        whereClauses.Add($"\"CardCode\" = '{filter.CardCode.Trim()}'");

                    if (!string.IsNullOrWhiteSpace(filter.DocumentStatus))
                    {
                        var stChar = string.Equals(filter.DocumentStatus, "bost_Close", StringComparison.OrdinalIgnoreCase) || filter.DocumentStatus == "C" ? "C" : "O";
                        whereClauses.Add($"\"DocStatus\" = '{stChar}'");
                    }

                    if (filter.FromDate.HasValue)
                        whereClauses.Add($"\"DocDate\" >= '{filter.FromDate.Value:yyyy-MM-dd}'");

                    if (filter.ToDate.HasValue)
                        whereClauses.Add($"\"DocDate\" <= '{filter.ToDate.Value:yyyy-MM-dd}'");

                    string whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";
                    string sql = $"SELECT TOP 100 \"DocEntry\", \"DocNum\", \"DocType\", \"ObjType\", \"DocDate\", \"DocDueDate\", \"TaxDate\", \"CardCode\", \"CardName\", \"DocTotal\", \"DocCur\", \"Comments\", \"DocStatus\", \"Series\", \"AtcEntry\", \"UserSign\", \"Confirmed\" FROM \"ODRF\" {whereSql} ORDER BY \"DocEntry\" DESC";

                    oRs.DoQuery(sql);
                    var list = new List<DraftDto>();

                    while (!oRs.EoF)
                    {
                        list.Add(new DraftDto
                        {
                            DocEntry = Convert.ToInt32(oRs.Fields.Item("DocEntry").Value),
                            DocNum = Convert.ToInt32(oRs.Fields.Item("DocNum").Value),
                            DocType = oRs.Fields.Item("DocType").Value?.ToString() == "I" ? "dDocument_Items" : "dDocument_Service",
                            DocObjectCode = oRs.Fields.Item("ObjType").Value?.ToString()?.Trim(),
                            DocDate = FormatSapDate(oRs.Fields.Item("DocDate").Value),
                            DocDueDate = FormatSapDate(oRs.Fields.Item("DocDueDate").Value),
                            TaxDate = FormatSapDate(oRs.Fields.Item("TaxDate").Value),
                            CardCode = oRs.Fields.Item("CardCode").Value?.ToString(),
                            CardName = oRs.Fields.Item("CardName").Value?.ToString(),
                            DocTotal = Convert.ToDecimal(oRs.Fields.Item("DocTotal").Value),
                            DocCurrency = oRs.Fields.Item("DocCur").Value?.ToString(),
                            Comments = oRs.Fields.Item("Comments").Value?.ToString(),
                            DocumentStatus = oRs.Fields.Item("DocStatus").Value?.ToString() == "O" ? "bost_Open" : "bost_Close",
                            Confirmed = oRs.Fields.Item("Confirmed").Value?.ToString() == "Y" ? "tYES" : "tNO",
                            Series = oRs.Fields.Item("Series").Value is DBNull ? null : Convert.ToInt32(oRs.Fields.Item("Series").Value),
                            AttachmentEntry = oRs.Fields.Item("AtcEntry").Value is DBNull ? null : (Convert.ToInt32(oRs.Fields.Item("AtcEntry").Value) == 0 ? null : Convert.ToInt32(oRs.Fields.Item("AtcEntry").Value)),
                            UserSign = oRs.Fields.Item("UserSign").Value is DBNull ? null : Convert.ToInt32(oRs.Fields.Item("UserSign").Value)
                        });
                        oRs.MoveNext();
                    }

                    return Task.FromResult<IEnumerable<DraftDto>>(list);
                }
                finally
                {
                    ComHelper.Release(oRs);
                }
            });
        }

        public async Task<(bool Success, int DocEntry, int? GeneratedDocEntry, string? ErrorMessage)> SaveDraftToDocumentAsync(UserSession session, int docEntry)
        {
            var connInfo = BuildConnectionInfo(session.CompanyDB, session.UserName, session.Password);

            _logger.LogInformation("DI API: Convirtiendo Borrador #{DocEntry} a Documento Real en SAP | DB: {DB} | Usuario: {User} | Operador: {AuditUser}",
                docEntry, session.CompanyDB, session.UserName, session.AuditUser ?? "N/A");

            return await _companyPool.ExecuteAsync(connInfo, company =>
            {
                Documents? oDraft = null;
                try
                {
                    oDraft = (Documents)company.GetBusinessObject(BoObjectTypes.oDrafts);

                    if (!oDraft.GetByKey(docEntry))
                    {
                        return Task.FromResult((false, docEntry, (int?)null, (string?)$"El borrador con DocEntry #{docEntry} no existe en SAP."));
                    }

                    int saveResult = oDraft.SaveDraftToDocument();

                    if (saveResult == 0)
                    {
                        string newKeyStr = company.GetNewObjectKey();
                        int.TryParse(newKeyStr, out int generatedDocEntry);
                        _logger.LogInformation("Borrador #{DocEntry} convertido exitosamente a Documento Real #{NewDocEntry} en SAP.", docEntry, generatedDocEntry);
                        return Task.FromResult((true, docEntry, (int?)generatedDocEntry, (string?)null));
                    }
                    else
                    {
                        company.GetLastError(out int lastErrorCode, out string lastErrorDescription);
                        _logger.LogError("Fallo al convertir borrador #{DocEntry} a documento real en SAP ({Code}): {Error}", docEntry, lastErrorCode, lastErrorDescription);
                        return Task.FromResult((false, docEntry, (int?)null, (string?)$"Error SAP ({lastErrorCode}): {lastErrorDescription}"));
                    }
                }
                finally
                {
                    ComHelper.Release(oDraft);
                }
            });
        }

        #endregion

        #region Users (Administración de Usuarios OUSR)

        public async Task<UserDto?> GetUserByIdAsync(UserSession session, int internalKey, bool includePermissions = false)
        {
            var connInfo = BuildConnectionInfo(session.CompanyDB, session.UserName, session.Password);

            return await _companyPool.ExecuteAsync(connInfo, company =>
            {
                Recordset? oRs = null;
                try
                {
                    oRs = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);
                    string sql = $"SELECT T0.* FROM \"OUSR\" T0 WHERE T0.\"USERID\" = {internalKey}";
                    oRs.DoQuery(sql);

                    if (oRs.EoF)
                    {
                        return Task.FromResult<UserDto?>(null);
                    }

                    var user = MapUserDtoFromRecordset(oRs);
                    EnrichUserAudit(company, user);

                    if (includePermissions)
                    {
                        user.UserPermissions = LoadUserPermissions(company, internalKey);
                    }

                    return Task.FromResult<UserDto?>(user);
                }
                finally
                {
                    ComHelper.Release(oRs);
                }
            });
        }

        public async Task<UserDto?> GetUserByCodeAsync(UserSession session, string userCode, bool includePermissions = false)
        {
            var connInfo = BuildConnectionInfo(session.CompanyDB, session.UserName, session.Password);

            return await _companyPool.ExecuteAsync(connInfo, company =>
            {
                Recordset? oRs = null;
                try
                {
                    oRs = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);
                    string escapedCode = userCode.Replace("'", "''");
                    string sql = $"SELECT T0.* FROM \"OUSR\" T0 WHERE T0.\"USER_CODE\" = '{escapedCode}'";
                    oRs.DoQuery(sql);

                    if (oRs.EoF)
                    {
                        return Task.FromResult<UserDto?>(null);
                    }

                    var user = MapUserDtoFromRecordset(oRs);
                    EnrichUserAudit(company, user);

                    if (includePermissions)
                    {
                        user.UserPermissions = LoadUserPermissions(company, user.InternalKey);
                    }

                    return Task.FromResult<UserDto?>(user);
                }
                finally
                {
                    ComHelper.Release(oRs);
                }
            });
        }

        public async Task<IEnumerable<UserDto>> GetUsersFilteredAsync(UserSession session, UserFilterDto filter)
        {
            var connInfo = BuildConnectionInfo(session.CompanyDB, session.UserName, session.Password);

            return await _companyPool.ExecuteAsync(connInfo, company =>
            {
                Recordset? oRs = null;
                try
                {
                    oRs = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);
                    var whereClauses = new List<string>();

                    if (!string.IsNullOrWhiteSpace(filter.Search))
                    {
                        string searchEscaped = filter.Search.Trim().Replace("'", "''");
                        whereClauses.Add($"(\"USER_CODE\" LIKE '%{searchEscaped}%' OR \"U_NAME\" LIKE '%{searchEscaped}%')");
                    }

                    if (!string.IsNullOrWhiteSpace(filter.Locked))
                    {
                        var isLocked = filter.Locked.Equals("tYES", StringComparison.OrdinalIgnoreCase) || filter.Locked.Equals("Y", StringComparison.OrdinalIgnoreCase) ? "Y" : "N";
                        whereClauses.Add($"\"Locked\" = '{isLocked}'");
                    }

                    if (!string.IsNullOrWhiteSpace(filter.Superuser))
                    {
                        var isSuper = filter.Superuser.Equals("tYES", StringComparison.OrdinalIgnoreCase) || filter.Superuser.Equals("Y", StringComparison.OrdinalIgnoreCase) ? "Y" : "N";
                        whereClauses.Add($"\"SUPERUSER\" = '{isSuper}'");
                    }

                    if (filter.Branch.HasValue)
                    {
                        whereClauses.Add($"\"Branch\" = {filter.Branch.Value}");
                    }

                    if (filter.Department.HasValue)
                    {
                        whereClauses.Add($"\"Department\" = {filter.Department.Value}");
                    }

                    string whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";
                    int pageSize = Math.Clamp(filter.PageSize, 1, 500);
                    int topLimit = pageSize * Math.Max(filter.Page, 1);

                    string sql = $"SELECT TOP {topLimit} T0.* FROM \"OUSR\" T0 {whereSql} ORDER BY T0.\"USERID\" ASC";
                    oRs.DoQuery(sql);

                    var list = new List<UserDto>();
                    int currentIndex = 0;
                    int startIndex = (Math.Max(filter.Page, 1) - 1) * pageSize;

                    while (!oRs.EoF)
                    {
                        if (currentIndex >= startIndex && list.Count < pageSize)
                        {
                            list.Add(MapUserDtoFromRecordset(oRs));
                        }
                        currentIndex++;
                        oRs.MoveNext();
                    }

                    return Task.FromResult<IEnumerable<UserDto>>(list);
                }
                finally
                {
                    ComHelper.Release(oRs);
                }
            });
        }

        public async Task<(bool Success, int InternalKey, string? ErrorMessage)> CreateUserAsync(UserSession session, CreateUserDto dto)
        {
            var connInfo = BuildConnectionInfo(session.CompanyDB, session.UserName, session.Password);

            _logger.LogInformation("DI API: Creando nuevo usuario '{UserCode}' en SAP | DB: {DB} | Operador: {AuditUser}",
                dto.UserCode, session.CompanyDB, session.AuditUser ?? session.UserName);

            return await _companyPool.ExecuteAsync(connInfo, company =>
            {
                SAPbobsCOM.Users? oUsers = null;
                try
                {
                    oUsers = (SAPbobsCOM.Users)company.GetBusinessObject(BoObjectTypes.oUsers);

                    oUsers.UserCode = dto.UserCode.Trim();
                    oUsers.UserName = dto.UserName ?? dto.UserCode;

                    if (!string.IsNullOrWhiteSpace(dto.UserPassword))
                    {
                        oUsers.UserPassword = dto.UserPassword;
                    }

                    if (!string.IsNullOrWhiteSpace(dto.Superuser))
                    {
                        oUsers.Superuser = dto.Superuser.Equals("tYES", StringComparison.OrdinalIgnoreCase) || dto.Superuser.Equals("Y", StringComparison.OrdinalIgnoreCase)
                            ? BoYesNoEnum.tYES : BoYesNoEnum.tNO;
                    }

                    if (!string.IsNullOrWhiteSpace(dto.Locked))
                    {
                        oUsers.Locked = dto.Locked.Equals("tYES", StringComparison.OrdinalIgnoreCase) || dto.Locked.Equals("Y", StringComparison.OrdinalIgnoreCase)
                            ? BoYesNoEnum.tYES : BoYesNoEnum.tNO;
                    }

                    if (!string.IsNullOrWhiteSpace(dto.Email))
                    {
                        oUsers.eMail = dto.Email;
                    }

                    if (!string.IsNullOrWhiteSpace(dto.MobilePhoneNumber))
                    {
                        oUsers.MobilePhoneNumber = dto.MobilePhoneNumber;
                    }

                    if (dto.Branch.HasValue)
                    {
                        oUsers.Branch = dto.Branch.Value;
                    }

                    if (dto.Department.HasValue)
                    {
                        oUsers.Department = dto.Department.Value;
                    }

                    if (!string.IsNullOrWhiteSpace(dto.Defaults))
                    {
                        oUsers.Defaults = dto.Defaults;
                    }

                    // Asignación de Campos de Usuario (UDFs)
                    SetUserUdfSafe(oUsers, "U_Establecimiento", dto.U_Establecimiento);
                    SetUserUdfSafe(oUsers, "U_visualizar_todos_DTE", dto.U_visualizar_todos_DTE);
                    SetUserUdfSafe(oUsers, "U_MultiEst", dto.U_MultiEst);
                    SetUserUdfSafe(oUsers, "U_MensajeEnvioDocto", dto.U_MensajeEnvioDocto);
                    SetUserUdfSafe(oUsers, "U_ActivarLog", dto.U_ActivarLog);
                    SetUserUdfSafe(oUsers, "U_ActivarXML", dto.U_ActivarXML);

                    if (dto.UserFields != null)
                    {
                        foreach (var kvp in dto.UserFields)
                        {
                            SetUserUdfSafe(oUsers, kvp.Key, kvp.Value);
                        }
                    }

                    int addResult = oUsers.Add();
                    if (addResult == 0)
                    {
                        string newKeyStr = company.GetNewObjectKey();
                        int.TryParse(newKeyStr, out int generatedKey);

                        UpdateUserSecurityFlags(company, generatedKey, dto.ChangePasswordNextLogon, dto.PasswordNeverExpires, _logger);

                        _logger.LogInformation("Usuario '{UserCode}' creado exitosamente con InternalKey #{Key}", dto.UserCode, generatedKey);
                        return Task.FromResult((true, generatedKey, (string?)null));
                    }
                    else
                    {
                        company.GetLastError(out int lastErrorCode, out string lastErrorDescription);
                        _logger.LogError("Fallo al crear usuario '{UserCode}' en SAP ({Code}): {Error}", dto.UserCode, lastErrorCode, lastErrorDescription);
                        return Task.FromResult((false, 0, (string?)$"Error SAP ({lastErrorCode}): {lastErrorDescription}"));
                    }
                }
                finally
                {
                    ComHelper.Release(oUsers);
                }
            });
        }

        public async Task<(bool Success, int InternalKey, string? ErrorMessage)> UpdateUserAsync(UserSession session, int internalKey, UpdateUserDto dto)
        {
            var connInfo = BuildConnectionInfo(session.CompanyDB, session.UserName, session.Password);

            _logger.LogInformation("DI API: Actualizando usuario #{InternalKey} en SAP | DB: {DB} | Operador: {AuditUser}",
                internalKey, session.CompanyDB, session.AuditUser ?? session.UserName);

            return await _companyPool.ExecuteAsync(connInfo, company =>
            {
                SAPbobsCOM.Users? oUsers = null;
                try
                {
                    oUsers = (SAPbobsCOM.Users)company.GetBusinessObject(BoObjectTypes.oUsers);

                    if (!oUsers.GetByKey(internalKey))
                    {
                        return Task.FromResult((false, internalKey, (string?)$"El usuario con InternalKey #{internalKey} no existe en SAP."));
                    }

                    if (!string.IsNullOrWhiteSpace(dto.UserName))
                    {
                        oUsers.UserName = dto.UserName;
                    }

                    if (!string.IsNullOrWhiteSpace(dto.Superuser))
                    {
                        oUsers.Superuser = dto.Superuser.Equals("tYES", StringComparison.OrdinalIgnoreCase) || dto.Superuser.Equals("Y", StringComparison.OrdinalIgnoreCase)
                            ? BoYesNoEnum.tYES : BoYesNoEnum.tNO;
                    }

                    if (!string.IsNullOrWhiteSpace(dto.Locked))
                    {
                        oUsers.Locked = dto.Locked.Equals("tYES", StringComparison.OrdinalIgnoreCase) || dto.Locked.Equals("Y", StringComparison.OrdinalIgnoreCase)
                            ? BoYesNoEnum.tYES : BoYesNoEnum.tNO;
                    }

                    if (dto.Email != null)
                    {
                        oUsers.eMail = dto.Email;
                    }

                    if (dto.MobilePhoneNumber != null)
                    {
                        oUsers.MobilePhoneNumber = dto.MobilePhoneNumber;
                    }

                    if (dto.Branch.HasValue)
                    {
                        oUsers.Branch = dto.Branch.Value;
                    }

                    if (dto.Department.HasValue)
                    {
                        oUsers.Department = dto.Department.Value;
                    }

                    if (dto.Defaults != null)
                    {
                        oUsers.Defaults = dto.Defaults;
                    }

                    // Actualización de UDFs
                    if (dto.U_Establecimiento != null) SetUserUdfSafe(oUsers, "U_Establecimiento", dto.U_Establecimiento);
                    if (dto.U_visualizar_todos_DTE != null) SetUserUdfSafe(oUsers, "U_visualizar_todos_DTE", dto.U_visualizar_todos_DTE);
                    if (dto.U_MultiEst != null) SetUserUdfSafe(oUsers, "U_MultiEst", dto.U_MultiEst);
                    if (dto.U_MensajeEnvioDocto != null) SetUserUdfSafe(oUsers, "U_MensajeEnvioDocto", dto.U_MensajeEnvioDocto);
                    if (dto.U_ActivarLog != null) SetUserUdfSafe(oUsers, "U_ActivarLog", dto.U_ActivarLog);
                    if (dto.U_ActivarXML != null) SetUserUdfSafe(oUsers, "U_ActivarXML", dto.U_ActivarXML);

                    if (dto.UserFields != null)
                    {
                        foreach (var kvp in dto.UserFields)
                        {
                            SetUserUdfSafe(oUsers, kvp.Key, kvp.Value);
                        }
                    }

                    int updateResult = oUsers.Update();
                    if (updateResult == 0)
                    {
                        UpdateUserSecurityFlags(company, internalKey, dto.ChangePasswordNextLogon, dto.PasswordNeverExpires, _logger);

                        _logger.LogInformation("Usuario #{InternalKey} actualizado exitosamente en SAP.", internalKey);
                        return Task.FromResult((true, internalKey, (string?)null));
                    }
                    else
                    {
                        company.GetLastError(out int lastErrorCode, out string lastErrorDescription);
                        _logger.LogError("Fallo al actualizar usuario #{InternalKey} en SAP ({Code}): {Error}", internalKey, lastErrorCode, lastErrorDescription);
                        return Task.FromResult((false, internalKey, (string?)$"Error SAP ({lastErrorCode}): {lastErrorDescription}"));
                    }
                }
                finally
                {
                    ComHelper.Release(oUsers);
                }
            });
        }

        public async Task<(bool Success, int InternalKey, string? ErrorMessage)> ChangeUserPasswordAsync(UserSession session, int internalKey, ChangeUserPasswordDto dto)
        {
            var connInfo = BuildConnectionInfo(session.CompanyDB, session.UserName, session.Password);

            _logger.LogInformation("DI API: Modificando contraseña para usuario #{InternalKey} en SAP | DB: {DB} | Operador: {AuditUser}",
                internalKey, session.CompanyDB, session.AuditUser ?? session.UserName);

            return await _companyPool.ExecuteAsync(connInfo, company =>
            {
                SAPbobsCOM.Users? oUsers = null;
                try
                {
                    oUsers = (SAPbobsCOM.Users)company.GetBusinessObject(BoObjectTypes.oUsers);

                    if (!oUsers.GetByKey(internalKey))
                    {
                        return Task.FromResult((false, internalKey, (string?)$"El usuario con InternalKey #{internalKey} no existe en SAP."));
                    }

                    if (!string.IsNullOrEmpty(dto.NewPassword))
                    {
                        oUsers.UserPassword = dto.NewPassword;
                    }

                    int updateResult = oUsers.Update();
                    if (updateResult == 0)
                    {
                        UpdateUserSecurityFlags(company, internalKey, dto.ChangePasswordNextLogon, dto.PasswordNeverExpires, _logger);

                        _logger.LogInformation("Contraseña del usuario #{InternalKey} cambiada exitosamente en SAP.", internalKey);
                        return Task.FromResult((true, internalKey, (string?)null));
                    }
                    else
                    {
                        company.GetLastError(out int lastErrorCode, out string lastErrorDescription);
                        _logger.LogError("Fallo al cambiar contraseña para usuario #{InternalKey} en SAP ({Code}): {Error}", internalKey, lastErrorCode, lastErrorDescription);
                        return Task.FromResult((false, internalKey, (string?)$"Error SAP ({lastErrorCode}): {lastErrorDescription}"));
                    }
                }
                finally
                {
                    ComHelper.Release(oUsers);
                }
            });
        }

        public async Task<(bool Success, int InternalKey, string? ErrorMessage)> UpdateUserByCodeAsync(UserSession session, string userCode, UpdateUserDto dto)
        {
            var user = await GetUserByCodeAsync(session, userCode);
            if (user == null)
            {
                return (false, 0, $"El usuario con código '{userCode}' no existe en la sociedad '{session.CompanyDB}'.");
            }

            return await UpdateUserAsync(session, user.InternalKey, dto);
        }

        public async Task<(bool Success, int InternalKey, string? ErrorMessage)> ChangeUserPasswordByCodeAsync(UserSession session, string userCode, ChangeUserPasswordDto dto)
        {
            var user = await GetUserByCodeAsync(session, userCode);
            if (user == null)
            {
                return (false, 0, $"El usuario con código '{userCode}' no existe en la sociedad '{session.CompanyDB}'.");
            }

            return await ChangeUserPasswordAsync(session, user.InternalKey, dto);
        }

        public async Task<List<CompanyDto>> GetSapCompaniesFromSrgcAsync(UserSession session)
        {
            var connInfo = BuildConnectionInfo(session.CompanyDB, session.UserName, session.Password);

            _logger.LogInformation("DI API: Consultando catálogo de sociedades en SAP (GetCompanyList / SRGC) | Operador: {AuditUser}",
                session.AuditUser ?? session.UserName);

            return await _companyPool.ExecuteAsync(connInfo, company =>
            {
                var list = new List<CompanyDto>();
                Recordset? oRs = null;

                // ESTRATEGIA 1: Método oficial nativo de la DI API (GetCompanyList)
                // Este método consulta el SLD / License Server y NO requiere permisos cross-schema en HANA.
                try
                {
                    oRs = company.GetCompanyList();
                    if (oRs != null)
                    {
                        while (!oRs.EoF)
                        {
                            string? dbName = null;
                            string? cmpName = null;

                            try { dbName = GetSafeString(oRs, "dbName") ?? GetSafeString(oRs, "DBName"); } catch { }
                            try { cmpName = GetSafeString(oRs, "cmpName") ?? GetSafeString(oRs, "CmpName") ?? dbName; } catch { }

                            // Si los campos no vienen nombrados, intentar por índice 0 y 1
                            if (string.IsNullOrWhiteSpace(dbName) && oRs.Fields.Count > 0)
                            {
                                dbName = oRs.Fields.Item(0).Value?.ToString();
                            }
                            if (string.IsNullOrWhiteSpace(cmpName) && oRs.Fields.Count > 1)
                            {
                                cmpName = oRs.Fields.Item(1).Value?.ToString();
                            }

                            if (!string.IsNullOrWhiteSpace(dbName))
                            {
                                list.Add(new CompanyDto
                                {
                                    SapDatabase = dbName,
                                    CompanyName = !string.IsNullOrWhiteSpace(cmpName) ? cmpName : dbName,
                                    IsActive = true
                                });
                            }
                            oRs.MoveNext();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("company.GetCompanyList() devolvió excepción: {Msg}. Intentando consultas alternativas...", ex.Message);
                }
                finally
                {
                    ComHelper.Release(oRs);
                    oRs = null;
                }

                if (list.Count > 0)
                {
                    _logger.LogInformation("GetCompanyList() obtuvo {Count} sociedades directamente desde SLD/SAP.", list.Count);
                    return Task.FromResult(list);
                }

                // ESTRATEGIA 2: Consulta directa a SBOCOMMON.SRGC
                try
                {
                    oRs = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);
                    string sql = "SELECT \"dbName\", \"cmpName\", \"LOC\", \"cmpStatus\" FROM \"SBOCOMMON\".\"SRGC\"";
                    oRs.DoQuery(sql);

                    while (!oRs.EoF)
                    {
                        var dbName = GetSafeString(oRs, "dbName") ?? string.Empty;
                        var cmpName = GetSafeString(oRs, "cmpName") ?? dbName;
                        var loc = GetSafeString(oRs, "LOC");
                        var status = GetSafeString(oRs, "cmpStatus");
                        bool isActive = status == "0" || string.IsNullOrEmpty(status);

                        if (!string.IsNullOrWhiteSpace(dbName))
                        {
                            list.Add(new CompanyDto
                            {
                                SapDatabase = dbName,
                                CompanyName = cmpName,
                                Localization = loc,
                                IsActive = isActive
                            });
                        }
                        oRs.MoveNext();
                    }
                }
                catch (Exception exSrgc)
                {
                    _logger.LogWarning("Consulta a SBOCOMMON.SRGC falló ({Msg}). Intentando SYS.SCHEMAS...", exSrgc.Message);
                }
                finally
                {
                    ComHelper.Release(oRs);
                    oRs = null;
                }

                if (list.Count > 0)
                {
                    return Task.FromResult(list);
                }

                // ESTRATEGIA 3: Consulta a SYS.SCHEMAS (catálogo de esquemas de HANA)
                try
                {
                    oRs = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);
                    string sql = "SELECT \"SCHEMA_NAME\" AS \"dbName\" FROM \"SYS\".\"SCHEMAS\" WHERE \"SCHEMA_NAME\" LIKE 'SBO%' OR \"SCHEMA_NAME\" LIKE 'SBODEMO%'";
                    oRs.DoQuery(sql);

                    while (!oRs.EoF)
                    {
                        var dbName = GetSafeString(oRs, "dbName") ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(dbName) && !dbName.Equals("SBOCOMMON", StringComparison.OrdinalIgnoreCase))
                        {
                            list.Add(new CompanyDto
                            {
                                SapDatabase = dbName,
                                CompanyName = dbName,
                                IsActive = true
                            });
                        }
                        oRs.MoveNext();
                    }
                }
                catch (Exception exSys)
                {
                    _logger.LogError(exSys, "Fallo al consultar SYS.SCHEMAS en HANA.");
                    throw new InvalidOperationException($"No se pudo obtener la lista de sociedades desde SAP (GetCompanyList / SRGC / SYS.SCHEMAS): {exSys.Message}");
                }
                finally
                {
                    ComHelper.Release(oRs);
                }

                return Task.FromResult(list);
            });
        }

        private static UserDto MapUserDtoFromRecordset(Recordset oRs)
        {
            var phone = GetSafeString(oRs, "PortNum")
                        ?? GetSafeString(oRs, "Tel1") 
                        ?? GetSafeString(oRs, "Tel2") 
                        ?? GetSafeString(oRs, "Cellular") 
                        ?? GetSafeString(oRs, "Mobile");

            var dto = new UserDto
            {
                InternalKey = GetSafeInt(oRs, "USERID"),
                UserCode = GetSafeString(oRs, "USER_CODE") ?? string.Empty,
                UserName = GetSafeString(oRs, "U_NAME"),
                Superuser = GetSafeString(oRs, "SUPERUSER") == "Y" ? "tYES" : "tNO",
                MobileUser = GetSafeString(oRs, "MobileUser") == "Y" ? "tYES" : "tNO",
                Locked = GetSafeString(oRs, "Locked") == "Y" ? "tYES" : "tNO",
                Email = GetSafeString(oRs, "E_Mail"),
                MobilePhoneNumber = phone,
                FaxNumber = GetSafeString(oRs, "Fax"),
                WindowsUserName = GetSafeString(oRs, "DomainUser"),
                MobileDeviceId = GetSafeString(oRs, "MobileIMEI"),
                Branch = GetSafeNullableInt(oRs, "Branch"),
                Department = GetSafeNullableInt(oRs, "Department"),
                Defaults = GetSafeString(oRs, "DfltsGroup"),
                Group = GetSafeString(oRs, "GROUPS") ?? GetSafeString(oRs, "UserGroup") ?? "ug_Regular",
                PasswordNeverExpires = GetSafeString(oRs, "PwdNeverEx") == "Y" ? "tYES" : "tNO",
                ChangePasswordNextLogon = GetSafeString(oRs, "OneLogPwd") == "Y" ? "tYES" : "tNO",
                LanguageCode = MapLanguageCode(GetSafeValue(oRs, "Language")),
                ScreenLockTime = GetSafeNullableInt(oRs, "ScreenLock"),
                EmployeeId = GetSafeNullableInt(oRs, "empID"),
                MaxDiscountGeneral = GetSafeNullableDouble(oRs, "DISCOUNT") ?? GetSafeNullableDouble(oRs, "MaxDiscnt") ?? 0.0,
                MaxDiscountSales = GetSafeNullableDouble(oRs, "SalesDisc") ?? GetSafeNullableDouble(oRs, "MaxDisSales") ?? 0.0,
                MaxDiscountPurchase = GetSafeNullableDouble(oRs, "PurchDisc") ?? GetSafeNullableDouble(oRs, "MaxDisPurch") ?? 0.0,
                LastLogoutDate = FormatSapDate(GetSafeValue(oRs, "LstLogoutD") ?? GetSafeValue(oRs, "LastLogoutDate") ?? GetSafeValue(oRs, "LastLogout")),
                LastLoginTime = FormatSapTime(GetSafeValue(oRs, "LstLoginT") ?? GetSafeValue(oRs, "LastLoginTime") ?? GetSafeValue(oRs, "LstLogTime")),
                LastLogoutTime = FormatSapTime(GetSafeValue(oRs, "LstLogoutT") ?? GetSafeValue(oRs, "LastLogoutTime")),
                LastPasswordChangeTime = FormatSapTime(GetSafeValue(oRs, "LstPwdChT") ?? GetSafeValue(oRs, "LastPasswordChangeTime")),
                LastPasswordChangedBy = GetSafeString(oRs, "LstPwdChB") ?? GetSafeString(oRs, "LastPasswordChangedBy"),
                U_Establecimiento = GetSafeString(oRs, "U_Establecimiento"),
                U_visualizar_todos_DTE = GetSafeString(oRs, "U_visualizar_todos_DTE") ?? "N",
                U_MultiEst = GetSafeString(oRs, "U_MultiEst") ?? "N",
                U_MensajeEnvioDocto = GetSafeString(oRs, "U_MensajeEnvioDocto") ?? "N",
                U_ActivarLog = GetSafeString(oRs, "U_ActivarLog") ?? "N",
                U_ActivarXML = GetSafeString(oRs, "U_ActivarXML") ?? "N"
            };

            return dto;
        }

        private static void EnrichUserAudit(Company company, UserDto dto)
        {
            Recordset? oRsAudit = null;
            try
            {
                oRsAudit = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string escapedUser = dto.UserCode.Replace("'", "''");
                string sql = $"SELECT TOP 20 \"Action\", \"ActionDate\", \"ActionTime\", \"ActionBy\" FROM \"USR5\" WHERE \"UserCode\" = '{escapedUser}' ORDER BY \"ActionDate\" DESC, \"ActionTime\" DESC";
                oRsAudit.DoQuery(sql);

                while (!oRsAudit.EoF)
                {
                    string action = oRsAudit.Fields.Item("Action").Value?.ToString() ?? string.Empty;
                    var actDate = FormatSapDate(oRsAudit.Fields.Item("ActionDate").Value);
                    var actTime = FormatSapTime(oRsAudit.Fields.Item("ActionTime").Value);
                    var actBy = oRsAudit.Fields.Item("ActionBy").Value?.ToString();

                    if ((action == "L" || action.Equals("actionLogin", StringComparison.OrdinalIgnoreCase)) && string.IsNullOrWhiteSpace(dto.LastLoginTime))
                    {
                        dto.LastLoginTime = actTime;
                    }
                    else if ((action == "O" || action.Equals("actionLogoff", StringComparison.OrdinalIgnoreCase)) && string.IsNullOrWhiteSpace(dto.LastLogoutDate))
                    {
                        dto.LastLogoutDate = actDate;
                        dto.LastLogoutTime = actTime;
                    }
                    else if ((action == "P" || action.Equals("actionPassword", StringComparison.OrdinalIgnoreCase)) && string.IsNullOrWhiteSpace(dto.LastPasswordChangeTime))
                    {
                        dto.LastPasswordChangeTime = actTime;
                        dto.LastPasswordChangedBy = actBy;
                    }

                    oRsAudit.MoveNext();
                }
            }
            catch
            {
                // USR5 opcional
            }
            finally
            {
                ComHelper.Release(oRsAudit);
            }
        }

        private static string? MapLanguageCode(object? langVal)
        {
            if (langVal == null || langVal is DBNull) return null;
            string strVal = langVal.ToString()?.Trim() ?? string.Empty;
            if (int.TryParse(strVal, out int langId))
            {
                return langId switch
                {
                    25 => "ln_Spanish_La",
                    24 => "ln_Spanish",
                    3 => "ln_English",
                    2 => "ln_Spanish_Ar",
                    5 => "ln_German",
                    8 => "ln_French",
                    9 => "ln_Italian",
                    23 => "ln_Portuguese_Br",
                    1 => "ln_Hebrew",
                    _ => strVal
                };
            }
            return strVal;
        }

        private static List<UserPermissionDto> LoadUserPermissions(Company company, int internalKey)
        {
            var perms = new List<UserPermissionDto>();
            Recordset? oRsPerms = null;
            try
            {
                oRsPerms = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);
                string sql = $"SELECT \"UserCode\", \"PermId\", \"Permission\" FROM \"USR3\" WHERE \"UserCode\" = {internalKey}";
                oRsPerms.DoQuery(sql);

                while (!oRsPerms.EoF)
                {
                    string permStr = oRsPerms.Fields.Item("Permission").Value?.ToString() ?? "N";
                    string mappedPerm = permStr == "F" ? "boper_Full" : (permStr == "R" ? "boper_ReadOnly" : "boper_None");

                    perms.Add(new UserPermissionDto
                    {
                        UserCode = Convert.ToInt32(oRsPerms.Fields.Item("UserCode").Value),
                        PermissionID = oRsPerms.Fields.Item("PermId").Value?.ToString() ?? string.Empty,
                        Permission = mappedPerm
                    });
                    oRsPerms.MoveNext();
                }
            }
            catch
            {
                // Ignorar si la tabla USR3 no está disponible
            }
            finally
            {
                ComHelper.Release(oRsPerms);
            }

            return perms;
        }

        private static void SetUserUdfSafe(SAPbobsCOM.Users oUsers, string fieldName, object? value)
        {
            if (value == null) return;
            try
            {
                Field? field = oUsers.UserFields.Fields.Item(fieldName);
                if (field != null)
                {
                    field.Value = value;
                    ComHelper.Release(field);
                }
            }
            catch
            {
                // Ignorar si el UDF no está configurado en SAP
            }
        }

        private static void UpdateUserSecurityFlags(Company company, int internalKey, string? changePasswordNextLogon, string? passwordNeverExpires, ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(changePasswordNextLogon) && string.IsNullOrWhiteSpace(passwordNeverExpires))
            {
                return;
            }

            Recordset? oRsFlags = null;
            try
            {
                oRsFlags = (Recordset)company.GetBusinessObject(BoObjectTypes.BoRecordset);
                var updates = new List<string>();
                if (!string.IsNullOrWhiteSpace(changePasswordNextLogon))
                {
                    string val = (changePasswordNextLogon.Equals("tYES", StringComparison.OrdinalIgnoreCase) || changePasswordNextLogon.Equals("Y", StringComparison.OrdinalIgnoreCase) || changePasswordNextLogon.Equals("true", StringComparison.OrdinalIgnoreCase)) ? "Y" : "N";
                    updates.Add($"\"OneLogPwd\" = '{val}'");
                }
                if (!string.IsNullOrWhiteSpace(passwordNeverExpires))
                {
                    string val = (passwordNeverExpires.Equals("tYES", StringComparison.OrdinalIgnoreCase) || passwordNeverExpires.Equals("Y", StringComparison.OrdinalIgnoreCase) || passwordNeverExpires.Equals("true", StringComparison.OrdinalIgnoreCase)) ? "Y" : "N";
                    updates.Add($"\"PassNever\" = '{val}'");
                }
                if (updates.Count > 0)
                {
                    string updateSql = $"UPDATE \"OUSR\" SET {string.Join(", ", updates)} WHERE \"USERID\" = {internalKey}";
                    oRsFlags.DoQuery(updateSql);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "No se pudieron actualizar los flags OneLogPwd/PassNever en OUSR para el usuario #{InternalKey}", internalKey);
            }
            finally
            {
                ComHelper.Release(oRsFlags);
            }
        }

        private static object? GetSafeValue(Recordset rs, string fieldName)
        {
            try
            {
                var val = rs.Fields.Item(fieldName).Value;
                return val is DBNull ? null : val;
            }
            catch
            {
                return null;
            }
        }

        private static string? GetSafeString(Recordset rs, string fieldName)
        {
            var val = GetSafeValue(rs, fieldName);
            return val?.ToString();
        }

        private static int GetSafeInt(Recordset rs, string fieldName)
        {
            var val = GetSafeValue(rs, fieldName);
            if (val == null) return 0;
            return Convert.ToInt32(val);
        }

        private static int? GetSafeNullableInt(Recordset rs, string fieldName)
        {
            var val = GetSafeValue(rs, fieldName);
            if (val == null) return null;
            return Convert.ToInt32(val);
        }

        private static double? GetSafeNullableDouble(Recordset rs, string fieldName)
        {
            var val = GetSafeValue(rs, fieldName);
            if (val == null) return null;
            return Convert.ToDouble(val);
        }

        #endregion

        #region Helpers de Formato y Mapeo SAP

        private static string? MapApprovalStatus(string? status)
        {
            return status?.ToUpperInvariant() switch
            {
                "Y" => "arsApproved",
                "N" => "arsNotApproved",
                "W" => "arsPending",
                "C" => "arsCanceled",
                "A" => "arsGenerated",
                _ => status
            };
        }

        private static string? MapLineStatus(string? status)
        {
            return status?.ToUpperInvariant() switch
            {
                "Y" => "ardApproved",
                "N" => "ardNotApproved",
                "W" => "ardPending",
                _ => status
            };
        }

        private static string? FormatSapDate(object? sapVal)
        {
            if (sapVal == null || sapVal is DBNull) return null;
            if (sapVal is DateTime dt)
            {
                if (dt <= new DateTime(1900, 1, 1) || dt == DateTime.MinValue) return null;
                return dt.ToString("yyyy-MM-dd");
            }
            return sapVal.ToString();
        }

        private static string? FormatSapTime(object? sapVal)
        {
            if (sapVal == null || sapVal is DBNull) return null;
            if (int.TryParse(sapVal.ToString(), out int timeInt))
            {
                string tStr = timeInt.ToString("D6");
                if (tStr.Length >= 6)
                {
                    return $"{tStr.Substring(0, 2)}:{tStr.Substring(2, 2)}:{tStr.Substring(4, 2)}";
                }
                if (tStr.Length >= 4)
                {
                    return $"{tStr.Substring(0, 2)}:{tStr.Substring(2, 2)}:00";
                }
            }
            return sapVal.ToString();
        }

        private static DateTime? CleanSapDate(DateTime dt)
        {
            if (dt <= new DateTime(1900, 1, 1) || dt == DateTime.MinValue)
            {
                return null;
            }
            return dt;
        }

        #endregion
    }
}
