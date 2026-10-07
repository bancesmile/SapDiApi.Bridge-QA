using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.BusinessPartners;
using SapDiApi.Bridge.Services.BusinessPartners;
using SapDiApi.Bridge.Services.Sap;
using Xunit;

namespace SapDiApi.Bridge.Tests.Unit.BusinessPartners
{
    public class BusinessPartnerServiceTests
    {
        private static UserSession CreateSession()
        {
            return new UserSession
            {
                SessionId = "QA-SESSION",
                CompanyDB = "SBODEMO_TEST",
                UserName = "manager",
                Password = "manager123",
                AuditUser = "qa@milesimo.com.gt",
                AuditApp = "QA-Automation"
            };
        }

        private static BusinessPartnerDto CreateBusinessPartner()
        {
            return new BusinessPartnerDto
            {
                CardCode = "C00001",
                CardName = "Cliente QA",
                CardType = "cCustomer",
                FederalTaxID = "1234567"
            };
        }

        // =====================================================
        // UT-036
        // Consulta sin sesion
        // =====================================================
        [Fact]
        public async Task GetByCardCode_WithoutSession_ReturnsNull()
        {
            var connector =
                new Mock<ISapDiApiConnector>();

            var service =
                new BusinessPartnerService(
                    connector.Object,
                    NullLogger<BusinessPartnerService>.Instance);

            var result =
                await service.GetByCardCodeAsync(
                    "C00001",
                    null);

            Assert.Null(result);

            connector.Verify(
                x => x.GetBusinessPartnerAsync(
                    It.IsAny<UserSession>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        // =====================================================
        // UT-037
        // Consulta con sesion
        // =====================================================
        [Fact]
        public async Task GetByCardCode_WithValidSession_ReturnsBusinessPartner()
        {
            var connector =
                new Mock<ISapDiApiConnector>();

            var expected =
                CreateBusinessPartner();

            connector
                .Setup(x => x.GetBusinessPartnerAsync(
                    It.Is<UserSession>(
                        s => s.CompanyDB == "SBODEMO_TEST"),
                    "C00001"))
                .ReturnsAsync(expected);

            var service =
                new BusinessPartnerService(
                    connector.Object,
                    NullLogger<BusinessPartnerService>.Instance);

            var result =
                await service.GetByCardCodeAsync(
                    "C00001",
                    CreateSession());

            Assert.NotNull(result);
            Assert.Equal("C00001", result.CardCode);
            Assert.Equal("Cliente QA", result.CardName);

            connector.Verify(
                x => x.GetBusinessPartnerAsync(
                    It.IsAny<UserSession>(),
                    "C00001"),
                Times.Once);
        }

        // =====================================================
        // UT-038
        // Crear sin sesion
        // =====================================================
        [Fact]
        public async Task Create_WithoutSession_ReturnsFalse()
        {
            var connector =
                new Mock<ISapDiApiConnector>();

            var service =
                new BusinessPartnerService(
                    connector.Object,
                    NullLogger<BusinessPartnerService>.Instance);

            var dto =
                CreateBusinessPartner();

            var result =
                await service.CreateAsync(
                    dto,
                    null);

            Assert.False(result.Success);
            Assert.Equal("C00001", result.CardCode);
            Assert.NotNull(result.ErrorMessage);

            connector.Verify(
                x => x.CreateBusinessPartnerAsync(
                    It.IsAny<UserSession>(),
                    It.IsAny<BusinessPartnerDto>()),
                Times.Never);
        }

        // =====================================================
        // UT-039
        // Crear con sesion
        // =====================================================
        [Fact]
        public async Task Create_WithValidSession_ReturnsSuccess()
        {
            var connector =
                new Mock<ISapDiApiConnector>();

            connector
                .Setup(x => x.CreateBusinessPartnerAsync(
                    It.IsAny<UserSession>(),
                    It.Is<BusinessPartnerDto>(
                        bp => bp.CardCode == "C00001")))
                .ReturnsAsync((
                    true,
                    "C00001",
                    (string?)null));

            var service =
                new BusinessPartnerService(
                    connector.Object,
                    NullLogger<BusinessPartnerService>.Instance);

            var result =
                await service.CreateAsync(
                    CreateBusinessPartner(),
                    CreateSession());

            Assert.True(result.Success);
            Assert.Equal("C00001", result.CardCode);
            Assert.Null(result.ErrorMessage);

            connector.Verify(
                x => x.CreateBusinessPartnerAsync(
                    It.IsAny<UserSession>(),
                    It.IsAny<BusinessPartnerDto>()),
                Times.Once);
        }

        // =====================================================
        // UT-040
        // Actualizar sin sesion
        // =====================================================
        [Fact]
        public async Task Update_WithoutSession_ReturnsFalse()
        {
            var connector =
                new Mock<ISapDiApiConnector>();

            var service =
                new BusinessPartnerService(
                    connector.Object,
                    NullLogger<BusinessPartnerService>.Instance);

            var result =
                await service.UpdateAsync(
                    "C00001",
                    CreateBusinessPartner(),
                    null);

            Assert.False(result.Success);
            Assert.Equal("C00001", result.CardCode);
            Assert.NotNull(result.ErrorMessage);

            connector.Verify(
                x => x.UpdateBusinessPartnerAsync(
                    It.IsAny<UserSession>(),
                    It.IsAny<string>(),
                    It.IsAny<BusinessPartnerDto>()),
                Times.Never);
        }

        // =====================================================
        // UT-041
        // Actualizar con sesion
        // =====================================================
        [Fact]
        public async Task Update_WithValidSession_ReturnsSuccess()
        {
            var connector =
                new Mock<ISapDiApiConnector>();

            connector
                .Setup(x => x.UpdateBusinessPartnerAsync(
                    It.IsAny<UserSession>(),
                    "C00001",
                    It.IsAny<BusinessPartnerDto>()))
                .ReturnsAsync((
                    true,
                    "C00001",
                    (string?)null));

            var service =
                new BusinessPartnerService(
                    connector.Object,
                    NullLogger<BusinessPartnerService>.Instance);

            var dto =
                CreateBusinessPartner();

            dto.CardName =
                "Cliente QA Actualizado";

            var result =
                await service.UpdateAsync(
                    "C00001",
                    dto,
                    CreateSession());

            Assert.True(result.Success);
            Assert.Equal("C00001", result.CardCode);
            Assert.Null(result.ErrorMessage);

            connector.Verify(
                x => x.UpdateBusinessPartnerAsync(
                    It.IsAny<UserSession>(),
                    "C00001",
                    It.Is<BusinessPartnerDto>(
                        bp =>
                            bp.CardName ==
                            "Cliente QA Actualizado")),
                Times.Once);
        }

        // =====================================================
        // UT-042
        // Queryable actual
        // =====================================================
        [Fact]
        public void GetBusinessPartnersQueryable_CurrentImplementation_ReturnsEmpty()
        {
            var connector =
                new Mock<ISapDiApiConnector>();

            var service =
                new BusinessPartnerService(
                    connector.Object,
                    NullLogger<BusinessPartnerService>.Instance);

            var result =
                service.GetBusinessPartnersQueryable();

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        // =====================================================
        // UT-043
        // Filtro actual
        // =====================================================
        [Fact]
        public async Task GetFiltered_CurrentImplementation_ReturnsEmpty()
        {
            var connector =
                new Mock<ISapDiApiConnector>();

            var service =
                new BusinessPartnerService(
                    connector.Object,
                    NullLogger<BusinessPartnerService>.Instance);

            var filter =
                new BusinessPartnerFilterDto();

            var result =
                await service.GetFilteredAsync(
                    filter,
                    CreateSession());

            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}