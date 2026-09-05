using System.Runtime.InteropServices;
using SAPbobsCOM;
using SapDiApi.Bridge.Models.Attachments;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.BusinessPartners;
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
                        var userFields = oBusinessPartner.UserFields.Fields;
                        for (int i = 0; i < userFields.Count; i++)
                        {
                            var field = userFields.Item(i);
                            var fieldName = field.Name.StartsWith("U_") ? field.Name : $"U_{field.Name}";
                            var val = field.Value;

                            if (val is DateTime dtVal)
                            {
                                bp.UserFields[fieldName] = CleanSapDate(dtVal);
                            }
                            else
                            {
                                bp.UserFields[fieldName] = val;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug("Aviso al mapear UserFields de BusinessPartners: {Message}", ex.Message);
                    }

                    // Mapeo de Direcciones (BPAddresses)
                    var addresses = oBusinessPartner.Addresses;
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

                    // Mapeo de Contactos (ContactEmployees)
                    var contacts = oBusinessPartner.ContactEmployees;
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
                                var contactUdfs = contacts.UserFields.Fields;
                                for (int u = 0; u < contactUdfs.Count; u++)
                                {
                                    var f = contactUdfs.Item(u);
                                    var fname = f.Name.StartsWith("U_") ? f.Name : $"U_{f.Name}";
                                    contactDto.UserFields[fname] = f.Value;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogDebug("Aviso UDF Contacto: {Message}", ex.Message);
                            }

                            bp.ContactEmployees.Add(contactDto);
                        }
                    }

                    // Mapeo de Cuentas Bancarias (BPBankAccounts)
                    try
                    {
                        var bpBankAccounts = oBusinessPartner.BPBankAccounts;
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
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug("Aviso al mapear BPBankAccounts: {Message}", ex.Message);
                    }

                    // Mapeo de Métodos de Pago (BPPaymentMethods)
                    try
                    {
                        var paymentMethods = oBusinessPartner.BPPaymentMethods;
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
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug("Aviso al mapear BPPaymentMethods: {Message}", ex.Message);
                    }

                    return Task.FromResult<BusinessPartnerDto?>(bp);
                }
                finally
                {
                    if (oBusinessPartner != null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Marshal.ReleaseComObject(oBusinessPartner);
                    }
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
                    if (oBusinessPartner != null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Marshal.ReleaseComObject(oBusinessPartner);
                    }
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
                    if (oBusinessPartner != null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Marshal.ReleaseComObject(oBusinessPartner);
                    }
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
                    if (oAttachment != null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Marshal.ReleaseComObject(oAttachment);
                    }
                }
            });
        }

        private static DateTime? CleanSapDate(DateTime dt)
        {
            if (dt <= new DateTime(1900, 1, 1) || dt == DateTime.MinValue)
            {
                return null;
            }
            return dt;
        }
    }
}
