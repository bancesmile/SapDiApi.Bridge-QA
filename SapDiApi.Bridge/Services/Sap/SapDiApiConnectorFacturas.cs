using SAPbobsCOM;
using SapDiApi.Bridge.Infrastructure.Security;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.Invoices;
using SapDiApi.Bridge.Models.Sap;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SapDiApi.Bridge.Services.Sap
{
    public class SapDiApiConnectorFacturas : ISapDiApiConnectorFacturas
    {
        private readonly ISapCompanyPool _companyPool;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SapDiApiConnectorFacturas> _logger;
        private Company sapConnector;
        public SapDiApiConnectorFacturas(
            ISapCompanyPool companyPool,
            IConfiguration configuration,
            ILogger<SapDiApiConnectorFacturas> logger)
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
        public async Task<(bool success, string message, int? docEntry, int? docNum)>
    CrearFacturaDeudoresSap(
        ApiClientConfig sessionKey,
        FacturaDeudoresDto request, string companyDB)
        {
            var session = new UserSession
            {
                CompanyDB = companyDB,
                UserName = "manager",
                Password = "M!l3$2300"
            };

            var connInfo = BuildConnectionInfo(
                session.CompanyDB,
                session.UserName,
                session.Password
            );

            try
            {
                return await _companyPool.ExecuteAsync(
                    connInfo,
                    company =>
                    {
                        Documents factura = null;
                        Documents facturaCreada = null;

                        try
                        {
                            // ==========================
                            // CREAR OBJETO FACTURA
                            // ==========================

                            factura = (Documents) company.GetBusinessObject(
                                BoObjectTypes.oInvoices
                            );

                            // ==========================
                            // CABECERA
                            // ==========================

                            factura.DocType =
                                BoDocumentTypes.dDocument_Items;

                            factura.CardCode = request.CardCode;

                            if (request.DocDate.HasValue)
                                factura.DocDate =
                                    request.DocDate.Value;

                            if (request.DocDueDate.HasValue)
                                factura.DocDueDate =
                                    request.DocDueDate.Value;

                            factura.TaxDate =
                                request.TaxDate;

                            factura.DocCurrency =
                                request.DocCurrency;

                            if (!string.IsNullOrWhiteSpace(
                                    request.Comments))
                            {
                                factura.Comments =
                                    request.Comments;
                            }

                            if (!string.IsNullOrWhiteSpace(
                                    request.JournalMemo))
                            {
                                factura.JournalMemo =
                                    request.JournalMemo;
                            }

                            if (!string.IsNullOrWhiteSpace(
                                    request.PayToCode))
                            {
                                factura.PayToCode =
                                    request.PayToCode;
                            }

                            if (request.Series > 0)
                            {
                                factura.Series =
                                    request.Series;
                            }

                            // ==========================
                            // UDF CABECERA
                            // ==========================

                            SetUserField(
                                factura,
                                "U_Nit",
                                request.U_Nit
                            );

                            SetUserField(
                                factura,
                                "U_Nombre",
                                request.U_Nombre
                            );

                            SetUserField(
                                factura,
                                "U_FE_Correos",
                                request.U_FE_Correos
                            );

                            SetUserField(
                                factura,
                                "U_FE_Status",
                                request.U_FE_Status
                            );

                            SetUserField(
                                factura,
                                "U_TipoDoctoSAT",
                                request.U_TipoDoctoSAT
                            );

                            SetUserField(
                                factura,
                                "U_DoctoFiscal",
                                request.U_DoctoFiscal
                            );

                            SetUserField(
                                factura,
                                "U_FE_Establecimiento",
                                request.U_FE_Establecimiento
                            );

                            SetUserField(
                                factura,
                                "U_Direccion",
                                request.U_Direccion
                            );

                            SetUserField(
                                factura,
                                "U_Inmueble",
                                request.U_Inmueble
                            );

                            SetUserField(
                                factura,
                                "U_Convenio",
                                request.U_Convenio
                            );

                            // ==========================
                            // VALIDAR DETALLE
                            // ==========================

                            if (request.DocumentLines == null ||
                                request.DocumentLines.Count == 0)
                            {
                                return Task.FromResult((
                                    success: false,
                                    message:
                                        "La factura debe contener al menos una línea.",
                                    docEntry: (int?) null,
                                    docNum: (int?) null
                                ));
                            }

                            // ==========================
                            // DETALLE
                            // ==========================

                            for (int i = 0;
                                 i < request.DocumentLines.Count;
                                 i++)
                            {
                                var linea =
                                    request.DocumentLines[i];

                                if (i > 0)
                                    factura.Lines.Add();

                                factura.Lines.ItemCode =
                                    linea.ItemCode;

                                if (!string.IsNullOrWhiteSpace(
                                        linea.ItemDescription))
                                {
                                    factura.Lines.ItemDescription =
                                        linea.ItemDescription;
                                }

                                factura.Lines.Quantity =
                                    linea.Quantity;

                                factura.Lines.UnitPrice =
                                    linea.Price;

                                if (!string.IsNullOrWhiteSpace(
                                        linea.Currency))
                                {
                                    factura.Lines.Currency =
                                        linea.Currency;
                                }

                                if (!string.IsNullOrWhiteSpace(
                                        linea.CostingCode))
                                {
                                    factura.Lines.CostingCode =
                                        linea.CostingCode;
                                }

                                if (!string.IsNullOrWhiteSpace(
                                        linea.TaxCode))
                                {
                                    factura.Lines.TaxCode =
                                        linea.TaxCode;
                                }

                                SetUserFieldLinea(
                                    factura.Lines,
                                    "U_Tipo",
                                    linea.U_Tipo
                                );

                                SetUserFieldLinea(
                                    factura.Lines,
                                    "U_Inmueble",
                                    linea.U_Inmueble
                                );
                            }

                            // ==========================
                            // TAX EXTENSION
                            // ==========================

                            if (request.TaxExtension != null)
                            {
                                var tax =
                                    factura.TaxExtension;

                                if (!string.IsNullOrWhiteSpace(
                                        request.TaxExtension.StreetB))
                                    tax.StreetB =
                                        request.TaxExtension.StreetB;

                                if (!string.IsNullOrWhiteSpace(
                                        request.TaxExtension.CityB))
                                    tax.CityB =
                                        request.TaxExtension.CityB;

                                if (!string.IsNullOrWhiteSpace(
                                        request.TaxExtension.CountyB))
                                    tax.CountyB =
                                        request.TaxExtension.CountyB;

                                if (!string.IsNullOrWhiteSpace(
                                        request.TaxExtension.StateB))
                                    tax.StateB =
                                        request.TaxExtension.StateB;

                                if (!string.IsNullOrWhiteSpace(
                                        request.TaxExtension.CountryB))
                                    tax.CountryB =
                                        request.TaxExtension.CountryB;
                            }

                            // ==========================
                            // CREAR EN SAP
                            // ==========================

                            int resultado =
                                factura.Add();

                            if (resultado != 0)
                            {
                                company.GetLastError(
                                    out int errorCode,
                                    out string errorMessage
                                );

                                return Task.FromResult((
                                    success: false,
                                    message:
                                        $"SAP {errorCode}: {errorMessage}",
                                    docEntry: (int?) null,
                                    docNum: (int?) null
                                ));
                            }

                            // ==========================
                            // OBTENER DOCENTRY
                            // ==========================

                            int docEntry =
                                Convert.ToInt32(
                                    company.GetNewObjectKey()
                                );

                            // ==========================
                            // OBTENER DOCNUM
                            // ==========================

                            int? docNum = null;

                            facturaCreada =
                                (Documents) company.GetBusinessObject(
                                    BoObjectTypes.oInvoices
                                );

                            if (facturaCreada.GetByKey(docEntry))
                            {
                                docNum =
                                    facturaCreada.DocNum;
                            }

                            return Task.FromResult((
                                success: true,
                                message:
                                    "Factura creada correctamente.",
                                docEntry: (int?) docEntry,
                                docNum: docNum
                            ));
                        }
                        catch (Exception ex)
                        {
                            return Task.FromResult((
                                success: false,
                                message: ex.Message,
                                docEntry: (int?) null,
                                docNum: (int?) null
                            ));
                        }
                        finally
                        {
                            if (facturaCreada != null)
                            {
                                Marshal.ReleaseComObject(
                                    facturaCreada
                                );
                            }

                            if (factura != null)
                            {
                                Marshal.ReleaseComObject(
                                    factura
                                );
                            }
                        }
                    }
                );
            }
            catch (Exception ex)
            {
                return (
                    success: false,
                    message:
                        $"Error conectando con SAP: {ex.Message}",
                    docEntry: null,
                    docNum: null
                );
            }
        }
        //    public Task<(bool success, string message, int? docEntry, int? docNum)>
        //CrearFacturaDeudoresSap(
        //    ApiClientConfig sessionKey,
        //    FacturaDeudoresDto request)
        //    {
        //        var session = new UserSession
        //        {
        //            CompanyDB = "TEST_SBO_ASOCSDCII_BI",
        //            UserName = "manager",
        //            Password = "M!l3$2300"
        //        };
        //        var connInfo = BuildConnectionInfo(session.CompanyDB, session.UserName, session.Password);

        //        return await _companyPool.ExecuteAsync(connInfo, company =>
        //        {
        //            Documents factura = null;

        //            try
        //            {
        //                factura = (Documents) company.GetBusinessObject(
        //                    BoObjectTypes.oInvoices
        //                );

        //                factura.DocType = BoDocumentTypes.dDocument_Items;
        //                factura.CardCode = request.CardCode;

        //                if (request.DocDate.HasValue)
        //                    factura.DocDate = request.DocDate.Value;

        //                if (request.DocDueDate.HasValue)
        //                    factura.DocDueDate = request.DocDueDate.Value;

        //                factura.TaxDate = request.TaxDate;

        //                factura.DocCurrency = request.DocCurrency;

        //                if (!string.IsNullOrWhiteSpace(request.Comments))
        //                    factura.Comments = request.Comments;

        //                if (!string.IsNullOrWhiteSpace(request.JournalMemo))
        //                    factura.JournalMemo = request.JournalMemo;

        //                if (!string.IsNullOrWhiteSpace(request.PayToCode))
        //                    factura.PayToCode = request.PayToCode;

        //                if (request.Series > 0)
        //                    factura.Series = request.Series;

        //                // ==========================
        //                // UDF CABECERA
        //                // ==========================

        //                SetUserField(factura, "U_Nit", request.U_Nit);
        //                SetUserField(factura, "U_Nombre", request.U_Nombre);
        //                SetUserField(factura, "U_FE_Correos", request.U_FE_Correos);
        //                SetUserField(factura, "U_FE_Status", request.U_FE_Status);
        //                SetUserField(factura, "U_TipoDoctoSAT", request.U_TipoDoctoSAT);
        //                SetUserField(factura, "U_DoctoFiscal", request.U_DoctoFiscal);
        //                SetUserField(factura, "U_FE_Establecimiento", request.U_FE_Establecimiento);

        //                SetUserField(factura, "U_Direccion", request.U_Direccion);
        //                SetUserField(factura, "U_Inmueble", request.U_Inmueble);
        //                SetUserField(factura, "U_Convenio", request.U_Convenio);

        //                // ==========================
        //                // DETALLE
        //                // ==========================

        //                if (request.DocumentLines == null ||
        //                    request.DocumentLines.Count == 0)
        //                {
        //                    return Task.FromResult((
        //                        success: false,
        //                        message: "La factura debe contener al menos una línea.",
        //                        docEntry: (int?) null,
        //                        docNum: (int?) null
        //                    ));
        //                }

        //                for (int i = 0; i < request.DocumentLines.Count; i++)
        //                {
        //                    var linea = request.DocumentLines[i];

        //                    if (i > 0)
        //                        factura.Lines.Add();

        //                    factura.Lines.ItemCode = linea.ItemCode;

        //                    if (!string.IsNullOrWhiteSpace(linea.ItemDescription))
        //                        factura.Lines.ItemDescription =
        //                            linea.ItemDescription;

        //                    factura.Lines.Quantity = linea.Quantity;
        //                    factura.Lines.UnitPrice = linea.Price;

        //                    if (!string.IsNullOrWhiteSpace(linea.Currency))
        //                        factura.Lines.Currency = linea.Currency;

        //                    if (!string.IsNullOrWhiteSpace(linea.CostingCode))
        //                        factura.Lines.CostingCode =
        //                            linea.CostingCode;

        //                    if (!string.IsNullOrWhiteSpace(linea.TaxCode))
        //                        factura.Lines.TaxCode =
        //                            linea.TaxCode;

        //                    SetUserFieldLinea(
        //                        factura.Lines,
        //                        "U_Tipo",
        //                        linea.U_Tipo
        //                    );

        //                    SetUserFieldLinea(
        //                        factura.Lines,
        //                        "U_Inmueble",
        //                        linea.U_Inmueble
        //                    );
        //                }

        //                // ==========================
        //                // TAX EXTENSION
        //                // ==========================

        //                if (request.TaxExtension != null)
        //                {
        //                    var tax = factura.TaxExtension;

        //                    if (!string.IsNullOrWhiteSpace(
        //                            request.TaxExtension.StreetB))
        //                        tax.StreetB =
        //                            request.TaxExtension.StreetB;

        //                    if (!string.IsNullOrWhiteSpace(
        //                            request.TaxExtension.CityB))
        //                        tax.CityB =
        //                            request.TaxExtension.CityB;

        //                    if (!string.IsNullOrWhiteSpace(
        //                            request.TaxExtension.CountyB))
        //                        tax.CountyB =
        //                            request.TaxExtension.CountyB;

        //                    if (!string.IsNullOrWhiteSpace(
        //                            request.TaxExtension.StateB))
        //                        tax.StateB =
        //                            request.TaxExtension.StateB;

        //                    if (!string.IsNullOrWhiteSpace(
        //                            request.TaxExtension.CountryB))
        //                        tax.CountryB =
        //                            request.TaxExtension.CountryB;
        //                }

        //                // ==========================
        //                // CREAR FACTURA
        //                // ==========================

        //                int resultado = factura.Add();

        //                if (resultado != 0)
        //                {
        //                    sapConnector.GetLastError(
        //                        out int errorCode,
        //                        out string errorMessage
        //                    );

        //                    return Task.FromResult((
        //                        success: false,
        //                        message:
        //                            $"SAP {errorCode}: {errorMessage}",
        //                        docEntry: (int?) null,
        //                        docNum: (int?) null
        //                    ));
        //                }

        //                int docEntry =
        //        Convert.ToInt32(company.GetNewObjectKey());

        //                return Task.FromResult((
        //                    success: true,
        //                    message: "Factura creada correctamente.",
        //                    docEntry: (int?) docEntry,
        //                    docNum: (int?) null
        //                ));
        //            }
        //            finally
        //            {
        //                if (factura != null)
        //                {
        //                    Marshal.ReleaseComObject(factura);
        //                }
        //            }
        //        }

        //        //    try
        //        //    {

        //        //    int docEntry =
        //        //        Convert.ToInt32(
        //        //            sapConnector.GetNewObjectKey()
        //        //        );

        //        //    // ==========================
        //        //    // OBTENER DOCNUM
        //        //    // ==========================

        //        //    Documents facturaCreada = null;
        //        //}
        //        //catch (Exception ex)
        //        //{
        //        //    return Task.FromResult((
        //        //        success: false,
        //        //        message: ex.Message,
        //        //        docEntry: (int?) null,
        //        //        docNum: (int?) null
        //        //    ));
        //        //}
        //        //finally
        //        //{
        //        //    if (factura != null)
        //        //    {
        //        //        Marshal.ReleaseComObject(factura);
        //        //        factura = null;
        //        //    }

        //        //    GC.Collect();
        //        //    GC.WaitForPendingFinalizers();
        //        //}
        //    }
        private void SetUserField(
            Documents documento,
            string campo,
            string valor)
        {
            if (!string.IsNullOrWhiteSpace(valor))
            {
                documento.UserFields
                    .Fields
                    .Item(campo)
                    .Value = valor;
            }
        }

        private void SetUserFieldLinea(
            Document_Lines linea,
            string campo,
            string valor)
        {
            if (!string.IsNullOrWhiteSpace(valor))
            {
                linea.UserFields
                    .Fields
                    .Item(campo)
                    .Value = valor;
            }
        }
    }
}
