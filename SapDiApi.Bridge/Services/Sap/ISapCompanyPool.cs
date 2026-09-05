using SAPbobsCOM;
using SapDiApi.Bridge.Models.Sap;

namespace SapDiApi.Bridge.Services.Sap
{
    public interface ISapCompanyPool
    {
        Task<T> ExecuteAsync<T>(SapConnectionInfo connInfo, Func<Company, Task<T>> action);
        int CurrentActiveConnections { get; }
        int MaxConcurrentConnections { get; }
    }
}
