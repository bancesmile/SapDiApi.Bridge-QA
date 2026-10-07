using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Services.Auth;
using SapDiApi.Bridge.Services.Sap;
using Xunit;

namespace SapDiApi.Bridge.Tests.Unit.Auth
{
    public class SapAuthServiceTests
    {
        // =====================================================
        // UT-023
        // CompanyDB obligatorio
        // =====================================================
        [Fact]
        public async Task ValidateCredentials_WithoutCompanyDb_ReturnsFalse()
        {
            // Arrange
            var connector = new Mock<ISapDiApiConnector>();

            var service = new SapAuthService(
                connector.Object,
                NullLogger<SapAuthService>.Instance);

            var request = new LoginRequestDto
            {
                CompanyDB = "",
                UserName = "manager",
                Password = "manager123"
            };

            // Act
            var result =
                await service.ValidateCredentialsAsync(request);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);

            connector.Verify(
                x => x.Connect(
                    It.IsAny<UserSession>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        // =====================================================
        // UT-024
        // UserName obligatorio
        // =====================================================
        [Fact]
        public async Task ValidateCredentials_WithoutUserName_ReturnsFalse()
        {
            var connector = new Mock<ISapDiApiConnector>();

            var service = new SapAuthService(
                connector.Object,
                NullLogger<SapAuthService>.Instance);

            var request = new LoginRequestDto
            {
                CompanyDB = "SBODEMO_TEST",
                UserName = "",
                Password = "manager123"
            };

            var result =
                await service.ValidateCredentialsAsync(request);

            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);

            connector.Verify(
                x => x.Connect(
                    It.IsAny<UserSession>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        // =====================================================
        // UT-025
        // Password obligatorio
        // =====================================================
        [Fact]
        public async Task ValidateCredentials_WithoutPassword_ReturnsFalse()
        {
            var connector = new Mock<ISapDiApiConnector>();

            var service = new SapAuthService(
                connector.Object,
                NullLogger<SapAuthService>.Instance);

            var request = new LoginRequestDto
            {
                CompanyDB = "SBODEMO_TEST",
                UserName = "manager",
                Password = ""
            };

            var result =
                await service.ValidateCredentialsAsync(request);

            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);

            connector.Verify(
                x => x.Connect(
                    It.IsAny<UserSession>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        // =====================================================
        // UT-026
        // Credenciales validas
        // =====================================================
        [Fact]
        public async Task ValidateCredentials_WithValidCredentials_ReturnsTrue()
        {
            var connector = new Mock<ISapDiApiConnector>();

            connector
                .Setup(x => x.Connect(
                    It.Is<UserSession>(
                        s =>
                            s.CompanyDB == "SBODEMO_TEST" &&
                            s.UserName == "manager"),
                    "manager123"))
                .Returns((true, string.Empty));

            var service = new SapAuthService(
                connector.Object,
                NullLogger<SapAuthService>.Instance);

            var request = new LoginRequestDto
            {
                CompanyDB = "SBODEMO_TEST",
                UserName = "manager",
                Password = "manager123"
            };

            var result =
                await service.ValidateCredentialsAsync(request);

            Assert.True(result.Success);
            Assert.Null(result.ErrorMessage);

            connector.Verify(
                x => x.Connect(
                    It.IsAny<UserSession>(),
                    "manager123"),
                Times.Once);
        }

        // =====================================================
        // UT-027
        // Credenciales invalidas
        // =====================================================
        [Fact]
        public async Task ValidateCredentials_WithInvalidCredentials_ReturnsConnectorError()
        {
            var connector = new Mock<ISapDiApiConnector>();

            connector
                .Setup(x => x.Connect(
                    It.IsAny<UserSession>(),
                    It.IsAny<string>()))
                .Returns((
                    false,
                    "Usuario o contraseña incorrectos"));

            var service = new SapAuthService(
                connector.Object,
                NullLogger<SapAuthService>.Instance);

            var request = new LoginRequestDto
            {
                CompanyDB = "SBODEMO_TEST",
                UserName = "manager",
                Password = "incorrecta"
            };

            var result =
                await service.ValidateCredentialsAsync(request);

            Assert.False(result.Success);

            Assert.Equal(
                "Usuario o contraseña incorrectos",
                result.ErrorMessage);
        }

        // =====================================================
        // UT-028
        // Error del conector sin mensaje
        // =====================================================
        [Fact]
        public async Task ValidateCredentials_WhenConnectorReturnsNoErrorMessage_ReturnsGenericError()
        {
            var connector = new Mock<ISapDiApiConnector>();

            connector
                .Setup(x => x.Connect(
                    It.IsAny<UserSession>(),
                    It.IsAny<string>()))
                .Returns((
                    false,
                    null!));

            var service = new SapAuthService(
                connector.Object,
                NullLogger<SapAuthService>.Instance);

            var request = new LoginRequestDto
            {
                CompanyDB = "SBODEMO_TEST",
                UserName = "manager",
                Password = "manager123"
            };

            var result =
                await service.ValidateCredentialsAsync(request);

            Assert.False(result.Success);

            Assert.Equal(
                "Credenciales de SAP incorrectas o servidor no disponible.",
                result.ErrorMessage);
        }
    }
}