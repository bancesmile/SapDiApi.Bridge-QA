using CapaComun;
using CapaEntidad.CE_SAP;
using Newtonsoft.Json;
using SAPbobsCOM;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace CapaDatos
{
    public class CD_SAPDIAPI
    {
        private Company ConectarSAP(CE_SAPConnection conexion)
        {
            Company company = new Company
            {
                Server = conexion.Server,
                language = BoSuppLangs.ln_English,
                LicenseServer = conexion.LicenseServer,
                CompanyDB = conexion.CompanyDB,
                UserName = conexion.UserName,
                Password = conexion.Password,
                DbServerType = BoDataServerTypes.dst_HANADB
            };

            int result = company.Connect();
            if (result != 0)
            {
                conexion.ErrorMessage = company.GetLastErrorDescription();
                conexion.Connected = false;
                throw new Exception($"Error de conexión ({result}): {conexion.ErrorMessage}");
            }

            conexion.Connected = true;
            return company;
        }

        public (bool result, string message) CrearProveedor(CE_SAPConnection conexion, CE_SAPProveedor proveedor)
        {
            Company company = null;
            BusinessPartners oBusinessPartner = null;
            try
            {
                company = ConectarSAP(conexion);

                oBusinessPartner =
                    (BusinessPartners)company.GetBusinessObject(BoObjectTypes.oBusinessPartners);

                // Propiedades básicas
                oBusinessPartner.CardCode = proveedor.CardCode;
                oBusinessPartner.CardName = proveedor.CardName;
                oBusinessPartner.CardType = BoCardTypes.cSupplier;
                oBusinessPartner.AdditionalID = proveedor.AdditionalID;
                oBusinessPartner.FederalTaxID = proveedor.FederalTaxID;
                oBusinessPartner.Series = proveedor.Series;
                oBusinessPartner.UnifiedFederalTaxID = proveedor.UnifiedFederalTaxID;
                oBusinessPartner.Currency = proveedor.Currency;
                oBusinessPartner.BillToState = proveedor.BillToState;
                oBusinessPartner.BilltoDefault = "FISCAL";
                oBusinessPartner.Address = proveedor.Address;

                if (!string.IsNullOrEmpty(proveedor.CardForeignName))
                    oBusinessPartner.CardForeignName = proveedor.CardForeignName;

                if (!string.IsNullOrEmpty(proveedor.Phone1))
                    oBusinessPartner.Phone1 = proveedor.Phone1;

                if (!string.IsNullOrEmpty(proveedor.EmailAddress))
                    oBusinessPartner.EmailAddress = proveedor.EmailAddress;

                if (proveedor.GroupCode.HasValue)
                    oBusinessPartner.GroupCode = proveedor.GroupCode.Value;

                if (!string.IsNullOrEmpty(proveedor.TerminoPago))
                    oBusinessPartner.PayTermsGrpCode = int.Parse(proveedor.TerminoPago);

                if (proveedor.PriceListNum.HasValue)
                    oBusinessPartner.PriceListNum = proveedor.PriceListNum.Value;

                if (!string.IsNullOrEmpty(proveedor.VatGroupLatinAmerica))
                    oBusinessPartner.VatGroupLatinAmerica = proveedor.VatGroupLatinAmerica;

                if (!string.IsNullOrEmpty(proveedor.VatLiable))
                {
                    if (proveedor.VatLiable == "vLiable")
                        oBusinessPartner.VatLiable = BoVatStatus.vLiable;
                    else if (proveedor.VatLiable == "vExempted")
                        oBusinessPartner.VatLiable = BoVatStatus.vExempted;
                }

                // Configuración de banco de la casa
                if (!string.IsNullOrEmpty(proveedor.HouseBank))
                    oBusinessPartner.HouseBank = proveedor.HouseBank;

                if (!string.IsNullOrEmpty(proveedor.HouseBankCountry))
                    oBusinessPartner.HouseBankCountry = proveedor.HouseBankCountry;

                if (!string.IsNullOrEmpty(proveedor.HouseBankAccount))
                    oBusinessPartner.HouseBankAccount = proveedor.HouseBankAccount;

                // Campos de usuario
                if (!string.IsNullOrEmpty(proveedor.U_TipoC))
                    oBusinessPartner.UserFields.Fields.Item("U_TipoC").Value = proveedor.U_TipoC;

                if (!string.IsNullOrEmpty(proveedor.TipoPago))
                    oBusinessPartner.UserFields.Fields.Item("U_Pago_Bancario").Value = proveedor.TipoPago;

                if (!string.IsNullOrEmpty(proveedor.U_PEP))
                    oBusinessPartner.UserFields.Fields.Item("U_PEP").Value = proveedor.U_PEP;

                if (!string.IsNullOrEmpty(proveedor.U_PEPC))
                    oBusinessPartner.UserFields.Fields.Item("U_PEPC").Value = proveedor.U_PEPC;

                if (!string.IsNullOrEmpty(proveedor.U_PEPF))
                    oBusinessPartner.UserFields.Fields.Item("U_PEPF").Value = proveedor.U_PEPF;

                if (!string.IsNullOrEmpty(proveedor.U_Regimen))
                    oBusinessPartner.UserFields.Fields.Item("U_Regimen").Value = proveedor.U_Regimen;

                if (!string.IsNullOrEmpty(proveedor.U_AgenteR))
                    oBusinessPartner.UserFields.Fields.Item("U_AgenteR").Value = proveedor.U_AgenteR;

                if (!string.IsNullOrEmpty(proveedor.U_Representante))
                    oBusinessPartner.UserFields.Fields.Item("U_Representante").Value = proveedor.U_Representante;

                if (!string.IsNullOrEmpty(proveedor.U_Tipo1))
                    oBusinessPartner.UserFields.Fields.Item("U_Tipo1").Value = proveedor.U_Tipo1;

                if (!string.IsNullOrEmpty(proveedor.U_Tipo2))
                    oBusinessPartner.UserFields.Fields.Item("U_Tipo2").Value = proveedor.U_Tipo2;

                if (!string.IsNullOrEmpty(proveedor.U_Tipo3))
                    oBusinessPartner.UserFields.Fields.Item("U_Tipo3").Value = proveedor.U_Tipo3;

                if (!string.IsNullOrEmpty(proveedor.U_NIT))
                    oBusinessPartner.UserFields.Fields.Item("U_NIT").Value = proveedor.U_NIT;

                if (!string.IsNullOrEmpty(proveedor.U_DPI))
                    oBusinessPartner.UserFields.Fields.Item("U_DPI").Value = proveedor.U_DPI;

                if (!string.IsNullOrEmpty(proveedor.U_Empleados))
                    oBusinessPartner.UserFields.Fields.Item("U_Empleados").Value = proveedor.U_Empleados;

                if (!string.IsNullOrEmpty(proveedor.U_Sucursales))
                    oBusinessPartner.UserFields.Fields.Item("U_Sucursales").Value = proveedor.U_Sucursales;

                if (!string.IsNullOrEmpty(proveedor.U_PROVENET))
                    oBusinessPartner.UserFields.Fields.Item("U_PROVENET").Value = proveedor.U_PROVENET;

                if (!string.IsNullOrEmpty(proveedor.U_Referencias))
                    oBusinessPartner.UserFields.Fields.Item("U_Referencias").Value = proveedor.U_Referencias;

                // Manejo de propiedades (Properties1 - Properties64)
                foreach (var prop in proveedor.Properties)
                {
                    if (prop.Key >= 1 && prop.Key <= 64)
                    {
                        oBusinessPartner.Properties[prop.Key] =
                            prop.Value ? BoYesNoEnum.tYES : BoYesNoEnum.tNO;
                    }
                }

                // Seteo de comentarios
                if (!string.IsNullOrEmpty(proveedor.FreeText))
                    oBusinessPartner.FreeText = proveedor.FreeText;

                // Configuracion de retencion de impuestos
                if (!string.IsNullOrEmpty(proveedor.SubjectToWithholdingTax))
                {
                    if (proveedor.SubjectToWithholdingTax == "boYES")
                    {
                        oBusinessPartner.SubjectToWithholdingTax = (BoYesNoNoneEnum)BoYesNoEnum.tYES;

                        // Agregar retenciones de impuestos usando la coleccion BPWithholdingTax
                        foreach (var retencion in proveedor.WithholdingTaxes)
                        {
                            if (!string.IsNullOrEmpty(retencion.WTCode))
                            {
                                // Si no es la primera retencion, añade una nueva linea
                                if (oBusinessPartner.BPWithholdingTax.Count > 0 &&
                                    !string.IsNullOrEmpty(oBusinessPartner.BPWithholdingTax.WTCode))
                                {
                                    oBusinessPartner.BPWithholdingTax.Add();
                                }

                                oBusinessPartner.BPWithholdingTax.SetCurrentLine(oBusinessPartner.BPWithholdingTax.Count - 1);
                                oBusinessPartner.BPWithholdingTax.WTCode = retencion.WTCode;
                            }
                        }
                    }
                    else
                    {
                        oBusinessPartner.SubjectToWithholdingTax = (BoYesNoNoneEnum)BoYesNoEnum.tNO;
                    }
                }

                // Agregar direcciones
                foreach (var direccion in proveedor.Direcciones)
                {
                    // Si no es la primera direccion, añade una nueva línea
                    if (oBusinessPartner.Addresses.Count > 0 && !string.IsNullOrEmpty(oBusinessPartner.Addresses.AddressName))
                    {
                        oBusinessPartner.Addresses.Add();
                    }

                    oBusinessPartner.Addresses.SetCurrentLine(oBusinessPartner.Addresses.Count - 1);

                    if (!string.IsNullOrEmpty(direccion.AddressName))
                        oBusinessPartner.Addresses.AddressName = direccion.AddressName;

                    if (!string.IsNullOrEmpty(direccion.Street))
                        oBusinessPartner.Addresses.Street = direccion.Street;

                    if (!string.IsNullOrEmpty(direccion.Block))
                        oBusinessPartner.Addresses.Block = direccion.Block;

                    if (!string.IsNullOrEmpty(direccion.ZipCode))
                        oBusinessPartner.Addresses.ZipCode = direccion.ZipCode;

                    if (!string.IsNullOrEmpty(direccion.City))
                    {
                        oBusinessPartner.Addresses.City = direccion.City;
                        oBusinessPartner.City = direccion.City;
                    }

                    if (!string.IsNullOrEmpty(direccion.County))
                    {
                        oBusinessPartner.Addresses.County = direccion.County;
                        oBusinessPartner.County = direccion.County;
                    }

                    if (!string.IsNullOrEmpty(direccion.Country))
                    {
                        oBusinessPartner.Addresses.Country = direccion.Country;
                    }

                    if (!string.IsNullOrEmpty(direccion.State))
                        oBusinessPartner.Addresses.State = direccion.State;

                    if (!string.IsNullOrEmpty(direccion.AddressType))
                    {
                        if (direccion.AddressType == "bo_BillTo")
                        {
                            oBusinessPartner.Addresses.AddressType = BoAddressType.bo_BillTo;
                            oBusinessPartner.BilltoDefault = direccion.AddressName;
                        }
                        else if (direccion.AddressType == "bo_ShipTo")
                        {
                            oBusinessPartner.Addresses.AddressType = BoAddressType.bo_ShipTo;
                            oBusinessPartner.ShipToDefault = direccion.AddressName;
                        }
                    }
                }

                // Agregar contactos
                foreach (var contacto in proveedor.Contactos)
                {
                    // Si no es el primer contacto, añade una nueva linea
                    if (oBusinessPartner.ContactEmployees.Count > 0 && !string.IsNullOrEmpty(oBusinessPartner.ContactEmployees.Name))
                    {
                        oBusinessPartner.ContactEmployees.Add();
                    }

                    oBusinessPartner.ContactEmployees.SetCurrentLine(oBusinessPartner.ContactEmployees.Count - 1);

                    if (!string.IsNullOrEmpty(contacto.Name))
                        oBusinessPartner.ContactEmployees.Name = contacto.Name;

                    if (!string.IsNullOrEmpty(contacto.FirstName))
                        oBusinessPartner.ContactEmployees.FirstName = contacto.FirstName;

                    if (!string.IsNullOrEmpty(contacto.LastName))
                        oBusinessPartner.ContactEmployees.LastName = contacto.LastName;

                    if (!string.IsNullOrEmpty(contacto.Position))
                        oBusinessPartner.ContactEmployees.Position = contacto.Position;

                    if (!string.IsNullOrEmpty(contacto.Phone1))
                        oBusinessPartner.ContactEmployees.Phone1 = contacto.Phone1;

                    if (!string.IsNullOrEmpty(contacto.E_Mail))
                        oBusinessPartner.ContactEmployees.E_Mail = contacto.E_Mail;

                    // Campos de usuario para contactos
                    if (!string.IsNullOrEmpty(contacto.U_Area))
                        oBusinessPartner.ContactEmployees.UserFields.Fields.Item("U_Area").Value = contacto.U_Area;

                    if (!string.IsNullOrEmpty(contacto.U_Tipo))
                        oBusinessPartner.ContactEmployees.UserFields.Fields.Item("U_Tipo").Value = contacto.U_Tipo;
                }

                // Agregar cuentas bancarias
                foreach (var cuentaBancaria in proveedor.CuentasBancarias)
                {
                    // Si no es la primera cuenta, añade una nueva linea
                    if (oBusinessPartner.BPBankAccounts.Count > 0 && !string.IsNullOrEmpty(oBusinessPartner.BPBankAccounts.AccountNo))
                    {
                        oBusinessPartner.BPBankAccounts.Add();
                    }

                    oBusinessPartner.BPBankAccounts.SetCurrentLine(oBusinessPartner.BPBankAccounts.Count - 1);

                    if (!string.IsNullOrEmpty(cuentaBancaria.AccountNo))
                        oBusinessPartner.BPBankAccounts.AccountNo = cuentaBancaria.AccountNo;

                    if (!string.IsNullOrEmpty(cuentaBancaria.BankCode))
                        oBusinessPartner.BPBankAccounts.BankCode = cuentaBancaria.BankCode;

                    if (!string.IsNullOrEmpty(cuentaBancaria.Country))
                        oBusinessPartner.BPBankAccounts.Country = cuentaBancaria.Country;

                    if (!string.IsNullOrEmpty(cuentaBancaria.AccountName))
                        oBusinessPartner.BPBankAccounts.AccountName = cuentaBancaria.AccountName;

                    if (!string.IsNullOrEmpty(cuentaBancaria.BICSwiftCode))
                        oBusinessPartner.BPBankAccounts.BICSwiftCode = cuentaBancaria.BICSwiftCode;

                    if (cuentaBancaria.ISRType.HasValue)
                        oBusinessPartner.BPBankAccounts.ISRType = cuentaBancaria.ISRType.Value;

                    // Campos de usuario para cuentas bancarias
                    if (!string.IsNullOrEmpty(cuentaBancaria.UserNo1))
                        oBusinessPartner.BPBankAccounts.UserFields.Fields.Item("UsrNumber1").Value = cuentaBancaria.UserNo1;

                    if (!string.IsNullOrEmpty(cuentaBancaria.UserNo2))
                        oBusinessPartner.BPBankAccounts.UserFields.Fields.Item("UsrNumber2").Value = cuentaBancaria.UserNo2;

                    if (!string.IsNullOrEmpty(cuentaBancaria.UserNo3))
                        oBusinessPartner.BPBankAccounts.UserFields.Fields.Item("UsrNumber3").Value = cuentaBancaria.UserNo3;
                }

                // Establecer la cuenta bancaria por defecto en el socio de negocio al crear
                if (proveedor.CuentasBancarias != null && proveedor.CuentasBancarias.Count > 0)
                {
                    var ultimaCuenta = proveedor.CuentasBancarias[proveedor.CuentasBancarias.Count - 1];
                    if (!string.IsNullOrEmpty(ultimaCuenta.AccountNo))
                        oBusinessPartner.DefaultAccount = ultimaCuenta.AccountNo;
                    if (!string.IsNullOrEmpty(ultimaCuenta.BankCode))
                        oBusinessPartner.DefaultBankCode = ultimaCuenta.BankCode;
                }

                // Agregar metodos de pago
                foreach (var metodoPago in proveedor.MetodosPago)
                {
                    // Si no es el primer metodo de pago, añade una nueva linea
                    if (oBusinessPartner.BPPaymentMethods.Count > 0 && !string.IsNullOrEmpty(oBusinessPartner.BPPaymentMethods.PaymentMethodCode))
                    {
                        oBusinessPartner.BPPaymentMethods.Add();
                    }
                    oBusinessPartner.BPPaymentMethods.SetCurrentLine(oBusinessPartner.BPPaymentMethods.Count - 1);
                    oBusinessPartner.BPPaymentMethods.PaymentMethodCode = metodoPago.Trim();
                }

                // Asociar anexo si existe
                if (proveedor.AnexoId > 0)
                {
                    oBusinessPartner.AttachmentEntry = proveedor.AnexoId;
                }


                // Guardar el proveedor
                int addResult = oBusinessPartner.Add();

                if (addResult == 0)
                {
                    return (true, string.Empty);
                }
                else
                {
                    // Serializar con formato legible (Indented)
                    string json = JsonConvert.SerializeObject(proveedor, Formatting.Indented);

                    conexion.ErrorMessage = company.GetLastErrorDescription();
                    string lastErrorDescription;
                    int lastErrorCode;
                    company.GetLastError(out lastErrorCode, out lastErrorDescription);
                    CC_Log.Logger.Log($"Proveedor '{proveedor.CardCode}' con error. Último mensaje de SAP: {lastErrorDescription} (Código: {lastErrorCode})");
                    CC_Log.Logger.Log($"Error al crear proveedor '{proveedor.CardCode}': {conexion.ErrorMessage}. Datos del proveedor:\n{json}");
                    string friendlyMessage = ObtenerMensajeAmigableSAP(lastErrorDescription, proveedor.CardCode, true);
                    return (false, friendlyMessage);
                }
            }
            catch (Exception ex)
            {
                conexion.ErrorMessage = ex.Message;
                return (false, ex.Message);
            }
            finally
            {
                if (oBusinessPartner != null)
                {
                    Marshal.ReleaseComObject(oBusinessPartner);
                    oBusinessPartner = null;
                }

                if (company != null)
                {
                    if (company.Connected)
                        company.Disconnect();

                    Marshal.ReleaseComObject(company);
                    company = null;
                }
            }
        }

        public (bool result, string message) ActualizarProveedor(CE_SAPConnection conexion, CE_SAPProveedor proveedor)
        {
            Company company = null;
            BusinessPartners oBusinessPartner = null;
            try
            {
                company = ConectarSAP(conexion);

                oBusinessPartner =
                    (BusinessPartners)company.GetBusinessObject(BoObjectTypes.oBusinessPartners);

                if (!oBusinessPartner.GetByKey(proveedor.CardCode))
                {
                    return (false, $"El proveedor con CardCode '{proveedor.CardCode}' no existe en SAP.");
                }

                // Propiedades basicas (CardCode es la llave)
                oBusinessPartner.CardName = proveedor.CardName;
                oBusinessPartner.AdditionalID = proveedor.AdditionalID;
                oBusinessPartner.FederalTaxID = proveedor.FederalTaxID;
                oBusinessPartner.UnifiedFederalTaxID = proveedor.UnifiedFederalTaxID;
                oBusinessPartner.Currency = proveedor.Currency;
                oBusinessPartner.BillToState = proveedor.BillToState;
                oBusinessPartner.Address = proveedor.Address;

                if (!string.IsNullOrEmpty(proveedor.CardForeignName))
                    oBusinessPartner.CardForeignName = proveedor.CardForeignName;

                if (!string.IsNullOrEmpty(proveedor.Phone1))
                    oBusinessPartner.Phone1 = proveedor.Phone1;

                if (!string.IsNullOrEmpty(proveedor.EmailAddress))
                    oBusinessPartner.EmailAddress = proveedor.EmailAddress;

                if (proveedor.GroupCode.HasValue)
                    oBusinessPartner.GroupCode = proveedor.GroupCode.Value;

                if (!string.IsNullOrEmpty(proveedor.TerminoPago))
                    oBusinessPartner.PayTermsGrpCode = int.Parse(proveedor.TerminoPago);

                if (proveedor.PriceListNum.HasValue)
                    oBusinessPartner.PriceListNum = proveedor.PriceListNum.Value;

                if (!string.IsNullOrEmpty(proveedor.VatGroupLatinAmerica))
                    oBusinessPartner.VatGroupLatinAmerica = proveedor.VatGroupLatinAmerica;

                if (!string.IsNullOrEmpty(proveedor.VatLiable))
                {
                    if (proveedor.VatLiable == "vLiable")
                        oBusinessPartner.VatLiable = BoVatStatus.vLiable;
                    else if (proveedor.VatLiable == "vExempted")
                        oBusinessPartner.VatLiable = BoVatStatus.vExempted;
                }

                // Configuracion de banco de la casa
                if (!string.IsNullOrEmpty(proveedor.HouseBank))
                    oBusinessPartner.HouseBank = proveedor.HouseBank;

                if (!string.IsNullOrEmpty(proveedor.HouseBankCountry))
                    oBusinessPartner.HouseBankCountry = proveedor.HouseBankCountry;

                if (!string.IsNullOrEmpty(proveedor.HouseBankAccount))
                    oBusinessPartner.HouseBankAccount = proveedor.HouseBankAccount;

                // Campos de usuario
                if (!string.IsNullOrEmpty(proveedor.U_TipoC))
                    oBusinessPartner.UserFields.Fields.Item("U_TipoC").Value = proveedor.U_TipoC;

                if (!string.IsNullOrEmpty(proveedor.TipoPago))
                    oBusinessPartner.UserFields.Fields.Item("U_Pago_Bancario").Value = proveedor.TipoPago;

                if (!string.IsNullOrEmpty(proveedor.U_PEP))
                    oBusinessPartner.UserFields.Fields.Item("U_PEP").Value = proveedor.U_PEP;

                if (!string.IsNullOrEmpty(proveedor.U_PEPC))
                    oBusinessPartner.UserFields.Fields.Item("U_PEPC").Value = proveedor.U_PEPC;

                if (!string.IsNullOrEmpty(proveedor.U_PEPF))
                    oBusinessPartner.UserFields.Fields.Item("U_PEPF").Value = proveedor.U_PEPF;

                if (!string.IsNullOrEmpty(proveedor.U_Regimen))
                    oBusinessPartner.UserFields.Fields.Item("U_Regimen").Value = proveedor.U_Regimen;

                if (!string.IsNullOrEmpty(proveedor.U_AgenteR))
                    oBusinessPartner.UserFields.Fields.Item("U_AgenteR").Value = proveedor.U_AgenteR;

                if (!string.IsNullOrEmpty(proveedor.U_Representante))
                    oBusinessPartner.UserFields.Fields.Item("U_Representante").Value = proveedor.U_Representante;

                if (!string.IsNullOrEmpty(proveedor.U_Tipo1))
                    oBusinessPartner.UserFields.Fields.Item("U_Tipo1").Value = proveedor.U_Tipo1;

                if (!string.IsNullOrEmpty(proveedor.U_Tipo2))
                    oBusinessPartner.UserFields.Fields.Item("U_Tipo2").Value = proveedor.U_Tipo2;

                if (!string.IsNullOrEmpty(proveedor.U_Tipo3))
                    oBusinessPartner.UserFields.Fields.Item("U_Tipo3").Value = proveedor.U_Tipo3;

                if (!string.IsNullOrEmpty(proveedor.U_NIT))
                    oBusinessPartner.UserFields.Fields.Item("U_NIT").Value = proveedor.U_NIT;

                if (!string.IsNullOrEmpty(proveedor.U_DPI))
                    oBusinessPartner.UserFields.Fields.Item("U_DPI").Value = proveedor.U_DPI;

                if (!string.IsNullOrEmpty(proveedor.U_Empleados))
                    oBusinessPartner.UserFields.Fields.Item("U_Empleados").Value = proveedor.U_Empleados;

                if (!string.IsNullOrEmpty(proveedor.U_Sucursales))
                    oBusinessPartner.UserFields.Fields.Item("U_Sucursales").Value = proveedor.U_Sucursales;

                if (!string.IsNullOrEmpty(proveedor.U_PROVENET))
                    oBusinessPartner.UserFields.Fields.Item("U_PROVENET").Value = proveedor.U_PROVENET;

                if (!string.IsNullOrEmpty(proveedor.U_Referencias))
                    oBusinessPartner.UserFields.Fields.Item("U_Referencias").Value = proveedor.U_Referencias;

                // Manejo de propiedades
                foreach (var prop in proveedor.Properties)
                {
                    if (prop.Key >= 1 && prop.Key <= 64)
                    {
                        oBusinessPartner.Properties[prop.Key] =
                            prop.Value ? BoYesNoEnum.tYES : BoYesNoEnum.tNO;
                    }
                }

                if (!string.IsNullOrEmpty(proveedor.FreeText))
                    oBusinessPartner.FreeText = proveedor.FreeText;

                // Configuracion de retencion de impuestos
                if (!string.IsNullOrEmpty(proveedor.SubjectToWithholdingTax))
                {
                    if (proveedor.SubjectToWithholdingTax == "boYES")
                    {
                        oBusinessPartner.SubjectToWithholdingTax = (BoYesNoNoneEnum)BoYesNoEnum.tYES;

                        // Agregar retenciones de impuestos nuevas
                        if (proveedor.WithholdingTaxes != null)
                        {
                            foreach (var retencion in proveedor.WithholdingTaxes)
                            {
                                if (!string.IsNullOrEmpty(retencion.WTCode))
                                {
                                    bool existeRetencion = false;
                                    for (int i = 0; i < oBusinessPartner.BPWithholdingTax.Count; i++)
                                    {
                                        oBusinessPartner.BPWithholdingTax.SetCurrentLine(i);
                                        if (oBusinessPartner.BPWithholdingTax.WTCode.Equals(retencion.WTCode, StringComparison.OrdinalIgnoreCase))
                                        {
                                            existeRetencion = true;
                                            break;
                                        }
                                    }

                                    if (!existeRetencion)
                                    {
                                        if (oBusinessPartner.BPWithholdingTax.Count > 0 &&
                                            !string.IsNullOrEmpty(oBusinessPartner.BPWithholdingTax.WTCode))
                                        {
                                            oBusinessPartner.BPWithholdingTax.Add();
                                        }
                                        oBusinessPartner.BPWithholdingTax.SetCurrentLine(oBusinessPartner.BPWithholdingTax.Count - 1);
                                        oBusinessPartner.BPWithholdingTax.WTCode = retencion.WTCode;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        oBusinessPartner.SubjectToWithholdingTax = (BoYesNoNoneEnum)BoYesNoEnum.tNO;
                    }
                }

                // Actualizar direcciones
                foreach (var direccion in proveedor.Direcciones)
                {
                    bool existeDireccion = false;
                    for (int i = 0; i < oBusinessPartner.Addresses.Count; i++)
                    {
                        oBusinessPartner.Addresses.SetCurrentLine(i);
                        if (oBusinessPartner.Addresses.AddressName.Equals(direccion.AddressName, StringComparison.OrdinalIgnoreCase))
                        {
                            existeDireccion = true;
                            break;
                        }
                    }

                    if (!existeDireccion)
                    {
                        if (oBusinessPartner.Addresses.Count > 0 && !string.IsNullOrEmpty(oBusinessPartner.Addresses.AddressName))
                        {
                            oBusinessPartner.Addresses.Add();
                        }
                        oBusinessPartner.Addresses.SetCurrentLine(oBusinessPartner.Addresses.Count - 1);
                        oBusinessPartner.Addresses.AddressName = direccion.AddressName;
                    }

                    if (!string.IsNullOrEmpty(direccion.Street))
                        oBusinessPartner.Addresses.Street = direccion.Street;

                    if (!string.IsNullOrEmpty(direccion.Block))
                        oBusinessPartner.Addresses.Block = direccion.Block;

                    if (!string.IsNullOrEmpty(direccion.ZipCode))
                        oBusinessPartner.Addresses.ZipCode = direccion.ZipCode;

                    if (!string.IsNullOrEmpty(direccion.City))
                    {
                        oBusinessPartner.Addresses.City = direccion.City;
                        oBusinessPartner.City = direccion.City;
                    }

                    if (!string.IsNullOrEmpty(direccion.County))
                    {
                        oBusinessPartner.Addresses.County = direccion.County;
                        oBusinessPartner.County = direccion.County;
                    }

                    if (!string.IsNullOrEmpty(direccion.Country))
                        oBusinessPartner.Addresses.Country = direccion.Country;

                    if (!string.IsNullOrEmpty(direccion.State))
                        oBusinessPartner.Addresses.State = direccion.State;

                    if (!string.IsNullOrEmpty(direccion.AddressType))
                    {
                        if (direccion.AddressType == "bo_BillTo")
                        {
                            oBusinessPartner.Addresses.AddressType = BoAddressType.bo_BillTo;
                            oBusinessPartner.BilltoDefault = direccion.AddressName;
                        }
                        else if (direccion.AddressType == "bo_ShipTo")
                        {
                            oBusinessPartner.Addresses.AddressType = BoAddressType.bo_ShipTo;
                            oBusinessPartner.ShipToDefault = direccion.AddressName;
                        }
                    }
                }

                // Desactivar contactos en SAP que ya no vienen en la lista del frontend para las areas clave
                for (int i = 0; i < oBusinessPartner.ContactEmployees.Count; i++)
                {
                    oBusinessPartner.ContactEmployees.SetCurrentLine(i);
                    string existingArea = oBusinessPartner.ContactEmployees.UserFields.Fields.Item("U_Area").Value?.ToString();

                    if (existingArea == "AreaComercial" || existingArea == "AreaContable" || existingArea == "EnvioRetenciones")
                    {
                        bool enFrontend = false;
                        foreach (var contacto in proveedor.Contactos)
                        {
                            if (existingArea.Equals(contacto.U_Area, StringComparison.OrdinalIgnoreCase))
                            {
                                enFrontend = true;
                                break;
                            }
                        }

                        if (!enFrontend)
                        {
                            oBusinessPartner.ContactEmployees.Active = BoYesNoEnum.tNO;
                        }
                        else
                        {
                            oBusinessPartner.ContactEmployees.Active = BoYesNoEnum.tYES;
                        }
                    }
                }

                // Actualizar contactos
                foreach (var contacto in proveedor.Contactos)
                {
                    bool existeContacto = false;
                    for (int i = 0; i < oBusinessPartner.ContactEmployees.Count; i++)
                    {
                        oBusinessPartner.ContactEmployees.SetCurrentLine(i);

                        // Primero intentar emparejar por el campo de usuario U_Area para no duplicar contactos del mismo departamento
                        if (!string.IsNullOrEmpty(contacto.U_Area))
                        {
                            string areaExistente = oBusinessPartner.ContactEmployees.UserFields.Fields.Item("U_Area").Value?.ToString();
                            if (contacto.U_Area.Equals(areaExistente, StringComparison.OrdinalIgnoreCase))
                            {
                                existeContacto = true;
                                break;
                            }
                        }

                        // Si no coincide el area, intentar emparejar por el Name (Nombre de contacto)
                        if (oBusinessPartner.ContactEmployees.Name.Equals(contacto.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            existeContacto = true;
                            break;
                        }
                    }

                    if (!existeContacto)
                    {
                        if (oBusinessPartner.ContactEmployees.Count > 0 && !string.IsNullOrEmpty(oBusinessPartner.ContactEmployees.Name))
                        {
                            oBusinessPartner.ContactEmployees.Add();
                        }
                        oBusinessPartner.ContactEmployees.SetCurrentLine(oBusinessPartner.ContactEmployees.Count - 1);
                        oBusinessPartner.ContactEmployees.Name = contacto.Name;
                    }

                    if (!string.IsNullOrEmpty(contacto.Name))
                        oBusinessPartner.ContactEmployees.Name = contacto.Name;

                    if (!string.IsNullOrEmpty(contacto.FirstName))
                        oBusinessPartner.ContactEmployees.FirstName = contacto.FirstName;

                    if (!string.IsNullOrEmpty(contacto.LastName))
                        oBusinessPartner.ContactEmployees.LastName = contacto.LastName;

                    if (!string.IsNullOrEmpty(contacto.Position))
                        oBusinessPartner.ContactEmployees.Position = contacto.Position;

                    if (!string.IsNullOrEmpty(contacto.Phone1))
                        oBusinessPartner.ContactEmployees.Phone1 = contacto.Phone1;

                    if (!string.IsNullOrEmpty(contacto.E_Mail))
                        oBusinessPartner.ContactEmployees.E_Mail = contacto.E_Mail;

                    if (!string.IsNullOrEmpty(contacto.U_Area))
                        oBusinessPartner.ContactEmployees.UserFields.Fields.Item("U_Area").Value = contacto.U_Area;

                    if (!string.IsNullOrEmpty(contacto.U_Tipo))
                        oBusinessPartner.ContactEmployees.UserFields.Fields.Item("U_Tipo").Value = contacto.U_Tipo;
                }

                // Actualizar cuenta bancaria (Sobrescribir la primera línea para no dejar historial)
                if (proveedor.CuentasBancarias != null && proveedor.CuentasBancarias.Count > 0)
                {
                    var cuentaBancaria = proveedor.CuentasBancarias[0];

                    if (oBusinessPartner.BPBankAccounts.Count == 0)
                    {
                        // Si no tiene ninguna línea, agregamos una
                        oBusinessPartner.BPBankAccounts.Add();
                        oBusinessPartner.BPBankAccounts.SetCurrentLine(0);
                    }
                    else
                    {
                        // Posicionar en la primera cuenta para sobrescribirla
                        oBusinessPartner.BPBankAccounts.SetCurrentLine(0);
                    }

                    // Asignar los campos de la cuenta bancaria
                    oBusinessPartner.BPBankAccounts.AccountNo = cuentaBancaria.AccountNo;

                    if (!string.IsNullOrEmpty(cuentaBancaria.BankCode))
                        oBusinessPartner.BPBankAccounts.BankCode = cuentaBancaria.BankCode;

                    if (!string.IsNullOrEmpty(cuentaBancaria.Country))
                        oBusinessPartner.BPBankAccounts.Country = cuentaBancaria.Country;

                    if (!string.IsNullOrEmpty(cuentaBancaria.AccountName))
                        oBusinessPartner.BPBankAccounts.AccountName = cuentaBancaria.AccountName;

                    if (!string.IsNullOrEmpty(cuentaBancaria.BICSwiftCode))
                        oBusinessPartner.BPBankAccounts.BICSwiftCode = cuentaBancaria.BICSwiftCode;

                    if (cuentaBancaria.ISRType.HasValue)
                        oBusinessPartner.BPBankAccounts.ISRType = cuentaBancaria.ISRType.Value;

                    if (!string.IsNullOrEmpty(cuentaBancaria.UserNo1))
                        oBusinessPartner.BPBankAccounts.UserFields.Fields.Item("UsrNumber1").Value = cuentaBancaria.UserNo1;

                    if (!string.IsNullOrEmpty(cuentaBancaria.UserNo2))
                        oBusinessPartner.BPBankAccounts.UserFields.Fields.Item("UsrNumber2").Value = cuentaBancaria.UserNo2;

                    if (!string.IsNullOrEmpty(cuentaBancaria.UserNo3))
                        oBusinessPartner.BPBankAccounts.UserFields.Fields.Item("UsrNumber3").Value = cuentaBancaria.UserNo3;

                    // Establecer la cuenta bancaria por defecto en el socio de negocio al actualizar
                    if (!string.IsNullOrEmpty(cuentaBancaria.AccountNo))
                        oBusinessPartner.DefaultAccount = cuentaBancaria.AccountNo;
                    if (!string.IsNullOrEmpty(cuentaBancaria.BankCode))
                        oBusinessPartner.DefaultBankCode = cuentaBancaria.BankCode;
                }

                // Actualizar metodos de pago
                foreach (var metodoPago in proveedor.MetodosPago)
                {
                    string metCode = metodoPago.Trim();
                    bool existeMetodo = false;
                    for (int i = 0; i < oBusinessPartner.BPPaymentMethods.Count; i++)
                    {
                        oBusinessPartner.BPPaymentMethods.SetCurrentLine(i);
                        if (oBusinessPartner.BPPaymentMethods.PaymentMethodCode.Equals(metCode, StringComparison.OrdinalIgnoreCase))
                        {
                            existeMetodo = true;
                            break;
                        }
                    }

                    if (!existeMetodo)
                    {
                        if (oBusinessPartner.BPPaymentMethods.Count > 0 && !string.IsNullOrEmpty(oBusinessPartner.BPPaymentMethods.PaymentMethodCode))
                        {
                            oBusinessPartner.BPPaymentMethods.Add();
                        }
                        oBusinessPartner.BPPaymentMethods.SetCurrentLine(oBusinessPartner.BPPaymentMethods.Count - 1);
                        oBusinessPartner.BPPaymentMethods.PaymentMethodCode = metCode;
                    }
                }

                // Asociar anexo si existe
                if (proveedor.AnexoId > 0)
                {
                    oBusinessPartner.AttachmentEntry = proveedor.AnexoId;
                }

                // Guardar cambios
                int updateResult = oBusinessPartner.Update();

                if (updateResult == 0)
                {
                    return (true, string.Empty);
                }
                else
                {
                    // Serializar con formato legible
                    string json = JsonConvert.SerializeObject(proveedor, Formatting.Indented);

                    string lastErrorDescription;
                    int lastErrorCode;
                    company.GetLastError(out lastErrorCode, out lastErrorDescription);
                    conexion.ErrorMessage = company.GetLastErrorDescription();
                    CC_Log.Logger.Log($"Error al actualizar proveedor '{proveedor.CardCode}': {conexion.ErrorMessage} (Código: {lastErrorCode}). Datos del proveedor:\n{json}");
                    string friendlyMessage = ObtenerMensajeAmigableSAP(lastErrorDescription, proveedor.CardCode, false);
                    return (false, friendlyMessage);
                }
            }
            catch (Exception ex)
            {
                conexion.ErrorMessage = ex.Message;
                return (false, ex.Message);
            }
            finally
            {
                if (oBusinessPartner != null)
                {
                    Marshal.ReleaseComObject(oBusinessPartner);
                    oBusinessPartner = null;
                }

                if (company != null)
                {
                    if (company.Connected)
                        company.Disconnect();

                    Marshal.ReleaseComObject(company);
                    company = null;
                }
            }
        }

        public (bool success, int anexoId) CrearAnexo(CE_SAPConnection conexion, CE_SAPAnexo anexo)
        {
            Company company = null;
            Attachments2 oAttachment = null;
            try
            {
                company = ConectarSAP(conexion);

                // Crear objeto de Attachments2
                oAttachment =
                    (Attachments2)company.GetBusinessObject(BoObjectTypes.oAttachments2);

                bool isUpdate = anexo.AnexoId > 0;
                if (isUpdate)
                {
                    if (!oAttachment.GetByKey(anexo.AnexoId))
                    {
                        throw new Exception($"No se pudo cargar el anexo existente con ID {anexo.AnexoId} para actualizarlo.");
                    }

                    // 1. Obtener la ruta activa/valida para esta empresa desde las líneas recibidas
                    string rutaValidaEmpresa = anexo.Lineas?.Find(l => !string.IsNullOrEmpty(l.SourcePath))?.SourcePath;

                    // 2. Barrido generico para corregir todas las líneas previas con rutas obsoletas (con comas o rutas viejas)
                    for (int i = 0; i < oAttachment.Lines.Count; i++)
                    {
                        oAttachment.Lines.SetCurrentLine(i);
                        string rutaActual = oAttachment.Lines.SourcePath;

                        if (!string.IsNullOrEmpty(rutaActual))
                        {
                            // Si contiene comas o es diferente a la ruta actual configurada
                            if (rutaActual.Contains(",") || (!string.IsNullOrEmpty(rutaValidaEmpresa) && !rutaActual.Equals(rutaValidaEmpresa, StringComparison.OrdinalIgnoreCase)))
                            {
                                string nuevaRuta = !string.IsNullOrEmpty(rutaValidaEmpresa)
                                    ? rutaValidaEmpresa
                                    : rutaActual.Replace(", ", " ").Replace(",", "");

                                oAttachment.Lines.SourcePath = nuevaRuta;
                                oAttachment.Lines.Override = BoYesNoEnum.tYES;
                            }
                        }
                    }
                }
                else
                {
                    // Configurar propiedades generales de anexos
                    oAttachment.Lines.Add();
                }

                // Procesar cada linea de anexo
                int newLinesAdded = 0;
                foreach (var linea in anexo.Lineas)
                {
                    bool lineExists = false;
                    int targetLineIndex = -1;

                    if (isUpdate)
                    {
                        for (int i = 0; i < oAttachment.Lines.Count; i++)
                        {
                            oAttachment.Lines.SetCurrentLine(i);
                            string nombreSinExt = Path.GetFileNameWithoutExtension(linea.FileName);

                            if (oAttachment.Lines.FileName.Equals(nombreSinExt, StringComparison.OrdinalIgnoreCase) ||
                                oAttachment.Lines.FileName.Equals(linea.FileName, StringComparison.OrdinalIgnoreCase))
                            {
                                lineExists = true;
                                targetLineIndex = i;
                                break;
                            }
                        }
                    }

                    if (lineExists)
                    {
                        // Posicionarse en la línea existente para actualizar su SourcePath y datos
                        oAttachment.Lines.SetCurrentLine(targetLineIndex);
                    }
                    else
                    {
                        // Agregar una nueva linea solo si no existe y no es la primera en modo creacion
                        if (isUpdate || newLinesAdded > 0)
                        {
                            oAttachment.Lines.Add();
                        }
                        newLinesAdded++;
                    }

                    // Configurar / Actualizar propiedades de la linea
                    if (!string.IsNullOrEmpty(linea.SourcePath))
                        oAttachment.Lines.SourcePath = linea.SourcePath;

                    if (!string.IsNullOrEmpty(linea.FileName))
                        oAttachment.Lines.FileName = linea.FileName;

                    if (!string.IsNullOrEmpty(linea.FileExtension))
                        oAttachment.Lines.FileExtension = linea.FileExtension;

                    // Forzar Override a tYES para que SAP actualice la referencia/archivo físico
                    oAttachment.Lines.Override = BoYesNoEnum.tYES;

                    if (!string.IsNullOrEmpty(linea.FreeText))
                        oAttachment.Lines.FreeText = linea.FreeText;

                    // Campos de usuario para anexos
                    if (!string.IsNullOrEmpty(linea.U_TipoDoc))
                        oAttachment.Lines.UserFields.Fields.Item("U_TipoDoc").Value = linea.U_TipoDoc;

                    if (!string.IsNullOrEmpty(linea.U_PCV))
                        oAttachment.Lines.UserFields.Fields.Item("U_PCV").Value = linea.U_PCV;

                    if (!string.IsNullOrEmpty(linea.U_Inmueble))
                        oAttachment.Lines.UserFields.Fields.Item("U_Inmueble").Value = linea.U_Inmueble;

                    if (!string.IsNullOrEmpty(linea.U_Docs))
                        oAttachment.Lines.UserFields.Fields.Item("U_Docs").Value = linea.U_Docs;
                }

                // Guardar/Actualizar anexo
                int addResult = isUpdate ? oAttachment.Update() : oAttachment.Add();

                if (addResult == 0)
                {
                    if (isUpdate)
                    {
                        return (true, anexo.AnexoId);
                    }
                    else
                    {
                        // Obtener el ID de anexo generado
                        string absoluteEntryStr = company.GetNewObjectKey();
                        if (int.TryParse(absoluteEntryStr, out int absoluteEntry))
                        {
                            return (true, absoluteEntry);
                        }
                        else
                        {
                            // Si el formato es diferente, intentar obtener el primer numero
                            return (true, int.Parse(absoluteEntryStr.Split('\t')[0]));
                        }
                    }
                }
                else
                {
                    conexion.ErrorMessage = company.GetLastErrorDescription();
                    return (false, 0);
                }
            }
            catch (Exception ex)
            {
                conexion.ErrorMessage = ex.Message;
                return (false, 0);
            }
            finally
            {
                if (oAttachment != null)
                {
                    Marshal.ReleaseComObject(oAttachment);
                    oAttachment = null;
                }

                if (company != null)
                {
                    if (company.Connected)
                        company.Disconnect();

                    Marshal.ReleaseComObject(company);
                    company = null;
                }
            }
        }

        public int ObtenerAttachmentEntrySocioNegocio(CE_SAPConnection conexion, string cardCode)
        {
            Company company = null;
            BusinessPartners oBusinessPartner = null;
            try
            {
                company = ConectarSAP(conexion);
                oBusinessPartner =
                    (BusinessPartners)company.GetBusinessObject(BoObjectTypes.oBusinessPartners);

                if (oBusinessPartner.GetByKey(cardCode))
                {
                    return oBusinessPartner.AttachmentEntry;
                }
                return 0;
            }
            catch (Exception ex)
            {
                CC_Log.Logger.LogException(ex);
                return 0;
            }
            finally
            {
                if (oBusinessPartner != null)
                {
                    Marshal.ReleaseComObject(oBusinessPartner);
                    oBusinessPartner = null;
                }

                if (company != null)
                {
                    if (company.Connected)
                        company.Disconnect();

                    Marshal.ReleaseComObject(company);
                    company = null;
                }
            }
        }

        private string ObtenerMensajeAmigableSAP(string originalError, string cardCode, bool esCreacion)
        {
            if (string.IsNullOrEmpty(originalError))
                return $"Error al {(esCreacion ? "crear" : "actualizar")} el proveedor '{cardCode}' en SAP.";

            // Interceptar el error 1300 de HANA (fetch returns more than requested number of rows)
            if (originalError.Contains("1300") ||
                originalError.IndexOf("fetch returns more than requested number of rows", StringComparison.OrdinalIgnoreCase) >= 0 ||
                originalError.IndexOf("SBO_SP_TRANSACTIONNOTIFICATION", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return $"Error al {(esCreacion ? "crear" : "actualizar")} el proveedor '{cardCode}': Existe un conflicto de consistencia de datos en la base de datos de SAP. Por favor, revise la información o comuníquese con el administrador del sistema.";
            }

            return originalError;
        }
    }
}