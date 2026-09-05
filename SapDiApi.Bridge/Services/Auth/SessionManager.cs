using System.Collections.Concurrent;
using SapDiApi.Bridge.Models.Auth;

namespace SapDiApi.Bridge.Services.Auth
{
    public class SessionManager : ISessionManager
    {
        private readonly ConcurrentDictionary<string, UserSession> _sessions = new(StringComparer.OrdinalIgnoreCase);
        private readonly ILogger<SessionManager> _logger;

        public SessionManager(ILogger<SessionManager> logger)
        {
            _logger = logger;
        }

        public int ActiveSessionCount => _sessions.Count(s => !s.Value.IsExpired);

        public UserSession CreateSession(
            string companyDb,
            string userName,
            string password,
            string? clientIp,
            int timeoutMinutes = 30,
            string? auditUser = null,
            string? auditApp = null,
            string executionMode = "Direct")
        {
            var session = new UserSession
            {
                SessionId = Guid.NewGuid().ToString("N"),
                CompanyDB = companyDb,
                UserName = userName,
                Password = password,
                AuditUser = auditUser,
                AuditApp = auditApp,
                ExecutionMode = executionMode,
                ClientIp = clientIp,
                CreatedAtUtc = DateTime.UtcNow,
                LastActivityUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(timeoutMinutes)
            };

            _sessions[session.SessionId] = session;
            _logger.LogInformation(
                "Nueva sesión SAP creada: {SessionId} | Usuario SAP: {User} | Operador: {AuditUser} | App: {App} | Modo: {Mode} | DB: {DB}",
                session.SessionId, userName, auditUser ?? "N/A", auditApp ?? "N/A", executionMode, companyDb);

            CleanExpiredSessions();

            return session;
        }

        public UserSession? GetSession(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return null;

            if (_sessions.TryGetValue(sessionId, out var session))
            {
                if (session.IsExpired)
                {
                    _sessions.TryRemove(sessionId, out _);
                    _logger.LogInformation("Sesión expirada removida: {SessionId}", sessionId);
                    return null;
                }

                return session;
            }

            return null;
        }

        public bool RefreshSession(string sessionId, int timeoutMinutes = 30)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return false;

            if (_sessions.TryGetValue(sessionId, out var session))
            {
                if (session.IsExpired)
                {
                    _sessions.TryRemove(sessionId, out _);
                    return false;
                }

                session.LastActivityUtc = DateTime.UtcNow;
                session.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(timeoutMinutes);
                return true;
            }

            return false;
        }

        public bool TerminateSession(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return false;

            var removed = _sessions.TryRemove(sessionId, out var session);
            if (removed && session != null)
            {
                _logger.LogInformation("Sesión cerrada manualmente (Logout): {SessionId} ({User})", sessionId, session.UserName);
            }

            return removed;
        }

        private void CleanExpiredSessions()
        {
            var now = DateTime.UtcNow;
            foreach (var kvp in _sessions)
            {
                if (kvp.Value.ExpiresAtUtc < now)
                {
                    _sessions.TryRemove(kvp.Key, out _);
                }
            }
        }
    }
}
