using SAPbobsCOM;
using SapDiApi.Bridge.Infrastructure.Security;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.Invoices;
using SapDiApi.Bridge.Services.BusinessPartners;
using SapDiApi.Bridge.Services.Sap;
using System.Globalization;
using System.Runtime.InteropServices;

namespace SapDiApi.Bridge.Services.Invoices
{
    public class FacturaDeudoresService : IFacturaDeudorService
    {
        private readonly ISapDiApiConnectorFacturas _sapConnectorFacturas;
        private readonly ILogger<FacturaDeudoresService> _logger;
        public FacturaDeudoresService(ISapDiApiConnectorFacturas sapConnectorFacturas, ILogger<FacturaDeudoresService> logger)
        {
            _sapConnectorFacturas = sapConnectorFacturas;
            _logger = logger;
        }

        public async Task<(bool success, string message, int? docEntry, int? docNum, string cardCode, string nit, string CardName)>
            CrearFacturaDeudoresAsync(FacturaDeudoresDto dto, ApiClientConfig? session = null, string companyDB = "")
        {
            try
            {
                if (session == null)
                {
                    return (false, "Se requiere una sesión activa (ApiKey) para crear facturas de deudores en SAP.", null, null, string.Empty, dto.U_Nit ?? string.Empty, dto.U_Nombre ?? string.Empty);
                }
                var (success, message, CardCode, CardName) = await _sapConnectorFacturas.ObtenerClientePorNit(companyDB, dto.U_Nit ?? string.Empty);

                if (!success)
                {
                    return (false, $"Error al obtener cliente por NIT: {message}", null, null, string.Empty, dto.U_Nit ?? string.Empty, dto.U_Nombre ?? string.Empty);
                }

                var result = await _sapConnectorFacturas.CrearFacturaDeudoresSap(session, dto, companyDB, CardCode ?? string.Empty, CardName ?? string.Empty);

                if (!result.success)
                {
                    return (false, $"Error: {result.message}", null, null, CardCode ?? string.Empty, result.nit, CardName ?? string.Empty);
                }

                return (true, "Factura deudores creada exitosamente.", result.docEntry, result.docNum, result.cardCode, result.nit, result.carName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear la factura deudores.");
                return (false, $"Error inesperado al crear la factura deudores: {ex.Message}", null, null, string.Empty, string.Empty, string.Empty);
            }
        }
    }
}
