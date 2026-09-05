using SapDiApi.Bridge.Models.Auth;

namespace SapDiApi.Bridge.Services.Auth
{
    public interface ISessionManager
    {
        UserSession CreateSession(string companyDb, string userName, string password, string? clientIp, int timeoutMinutes = 30, string? auditUser = null, string? auditApp = null, string executionMode = "Direct");
        UserSession? GetSession(string sessionId);
        bool RefreshSession(string sessionId, int timeoutMinutes = 30);
        bool TerminateSession(string sessionId);
        int ActiveSessionCount { get; }
    }
}
