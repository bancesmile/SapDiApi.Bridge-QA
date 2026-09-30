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

        public async Task<(bool success, string message, int? docEntry, int? docNum)>
            CrearFacturaDeudoresAsync(FacturaDeudoresDto dto, ApiClientConfig? session = null)
        {
            try
            {
                if (session == null)
                {
                    return (false, "Se requiere una sesión activa (B1SESSION) para crear socios de negocio en SAP.", null, null);
                }
                var result = await _sapConnectorFacturas.CrearFacturaDeudoresSap(session,dto);

                if (!result.success)
                {
                    return (false, $"Error: {result.message}", null, null);
                }

                return (true, "Factura deudores creada exitosamente.", result.docEntry, result.docNum);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear la factura deudores.");
                return (false, $"Error inesperado al crear la factura deudores: {ex.Message}", null, null);
            }
        }

        private DateTime ConvertirFecha(string fecha)
        {
            if (DateTime.TryParse(
                fecha,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime resultado))
            {
                return resultado;
            }

            throw new Exception(
                $"La fecha '{fecha}' no tiene un formato válido."
            );
        }

        private double ConvertirDouble(decimal valor)
        {
            if (double.TryParse(
                valor.ToString(),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out double resultado))
            {
                return resultado;
            }

            throw new Exception(
                $"El valor '{valor}' no es un número válido."
            );
        }
    }
}
