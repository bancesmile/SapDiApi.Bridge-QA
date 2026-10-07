using Microsoft.Extensions.Logging.Abstractions;
using SapDiApi.Bridge.Services.Auth;
using Xunit;

namespace SapDiApi.Bridge.Tests.Unit.Auth
{
    public class SessionManagerTests
    {
        private static SessionManager CreateSessionManager()
        {
            return new SessionManager(
                NullLogger<SessionManager>.Instance);
        }

        // =====================================================
        // UT-001
        // Crear sesion correctamente
        // =====================================================
        [Fact]
        public void CreateSession_WithValidData_CreatesSession()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            // Act
            var session = sessionManager.CreateSession(
                companyDb: "SBODEMO_TEST",
                userName: "manager",
                password: "manager123",
                clientIp: "127.0.0.1",
                timeoutMinutes: 30,
                auditUser: "qa@milesimo.com.gt",
                auditApp: "QA-Automation");

            // Assert
            Assert.NotNull(session);
            Assert.False(string.IsNullOrWhiteSpace(session.SessionId));
            Assert.Equal("SBODEMO_TEST", session.CompanyDB);
            Assert.Equal("manager", session.UserName);
            Assert.Equal("127.0.0.1", session.ClientIp);
            Assert.Equal("qa@milesimo.com.gt", session.AuditUser);
            Assert.Equal("QA-Automation", session.AuditApp);
        }

        // =====================================================
        // UT-002
        // Obtener sesion existente
        // =====================================================
        [Fact]
        public void GetSession_WithExistingSession_ReturnsSession()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            var createdSession = sessionManager.CreateSession(
                "SBODEMO_TEST",
                "manager",
                "manager123",
                "127.0.0.1");

            // Act
            var result =
                sessionManager.GetSession(
                    createdSession.SessionId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(
                createdSession.SessionId,
                result.SessionId);
        }

        // =====================================================
        // UT-003
        // Obtener sesion inexistente
        // =====================================================
        [Fact]
        public void GetSession_WithUnknownSession_ReturnsNull()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            // Act
            var result =
                sessionManager.GetSession(
                    "SESSION_NO_EXISTENTE");

            // Assert
            Assert.Null(result);
        }

        // =====================================================
        // UT-004
        // SessionId vacio
        // =====================================================
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void GetSession_WithEmptySessionId_ReturnsNull(
            string sessionId)
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            // Act
            var result =
                sessionManager.GetSession(sessionId);

            // Assert
            Assert.Null(result);
        }

        // =====================================================
        // UT-005
        // Refrescar sesion valida
        // =====================================================
        [Fact]
        public void RefreshSession_WithValidSession_ReturnsTrue()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            var session = sessionManager.CreateSession(
                "SBODEMO_TEST",
                "manager",
                "manager123",
                "127.0.0.1",
                timeoutMinutes: 30);

            var previousExpiration =
                session.ExpiresAtUtc;

            // Act
            var result =
                sessionManager.RefreshSession(
                    session.SessionId,
                    timeoutMinutes: 60);

            // Assert
            Assert.True(result);

            var refreshedSession =
                sessionManager.GetSession(
                    session.SessionId);

            Assert.NotNull(refreshedSession);

            Assert.True(
                refreshedSession.ExpiresAtUtc >
                previousExpiration);
        }

        // =====================================================
        // UT-006
        // Refrescar sesion inexistente
        // =====================================================
        [Fact]
        public void RefreshSession_WithUnknownSession_ReturnsFalse()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            // Act
            var result =
                sessionManager.RefreshSession(
                    "SESSION_NO_EXISTENTE");

            // Assert
            Assert.False(result);
        }

        // =====================================================
        // UT-007
        // Terminar sesion
        // =====================================================
        [Fact]
        public void TerminateSession_WithValidSession_RemovesSession()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            var session = sessionManager.CreateSession(
                "SBODEMO_TEST",
                "manager",
                "manager123",
                "127.0.0.1");

            // Act
            var terminated =
                sessionManager.TerminateSession(
                    session.SessionId);

            var result =
                sessionManager.GetSession(
                    session.SessionId);

            // Assert
            Assert.True(terminated);
            Assert.Null(result);
        }

        // =====================================================
        // UT-008
        // Sesion expirada
        // =====================================================
        [Fact]
        public void GetSession_WithExpiredSession_ReturnsNull()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            var session = sessionManager.CreateSession(
                "SBODEMO_TEST",
                "manager",
                "manager123",
                "127.0.0.1",
                timeoutMinutes: -1);

            // Act
            var result =
                sessionManager.GetSession(
                    session.SessionId);

            // Assert
            Assert.Null(result);
        }

        // =====================================================
        // UT-009
        // Contador de sesiones
        // =====================================================
        [Fact]
        public void ActiveSessionCount_WithActiveSessions_ReturnsCorrectCount()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            sessionManager.CreateSession(
                "SBODEMO_TEST",
                "manager",
                "manager123",
                "127.0.0.1");

            sessionManager.CreateSession(
                "SBODEMO_TEST",
                "usuario2",
                "password2",
                "127.0.0.2");

            // Act
            var count =
                sessionManager.ActiveSessionCount;

            // Assert
            Assert.Equal(2, count);
        }

        // =====================================================
        // UT-010
        // SessionId debe ser unico
        // =====================================================
        [Fact]
        public void CreateSession_MultipleSessions_GenerateUniqueIds()
        {
            // Arrange
            var sessionManager = CreateSessionManager();

            // Act
            var session1 = sessionManager.CreateSession(
                "SBODEMO_TEST",
                "manager",
                "manager123",
                "127.0.0.1");

            var session2 = sessionManager.CreateSession(
                "SBODEMO_TEST",
                "manager",
                "manager123",
                "127.0.0.1");

            // Assert
            Assert.NotEqual(
                session1.SessionId,
                session2.SessionId);
        }
    }
}