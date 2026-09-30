using SapDiApi.Bridge.Infrastructure.Security;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.Drafts;
using SapDiApi.Bridge.Models.Invoices;

namespace SapDiApi.Bridge.Services.Sap
{
    public interface ISapDiApiConnectorFacturas
    {
        Task<(bool success, string message, int? docEntry, int? docNum)> CrearFacturaDeudoresSap(
                ApiClientConfig session, FacturaDeudoresDto request, string companyDB);
    }
}
