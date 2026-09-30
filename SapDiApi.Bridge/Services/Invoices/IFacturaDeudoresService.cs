using SapDiApi.Bridge.Infrastructure.Security;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.Invoices;

namespace SapDiApi.Bridge.Services.Invoices
{
    public interface IFacturaDeudorService
    {
        Task<(bool success, string message, int? docEntry, int? docNum)> CrearFacturaDeudoresAsync(
                FacturaDeudoresDto request, ApiClientConfig? session = null, string companyDB = "");
    }
}
