using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.Drafts;
using SapDiApi.Bridge.Services.Drafts;
using SapDiApi.Bridge.Services.Sap;
using Xunit;

namespace SapDiApi.Bridge.Tests.Unit.Drafts
{
    public class DraftServiceTests
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

        // =====================================================
        // UT-052
        // Consultar Draft sin sesion
        // =====================================================
        [Fact]
        public async Task GetByDocEntry_WithoutSession_ReturnsNull()
        {
            var connector =
                new Mock<ISapDiApiConnector>();

            var service =
                new DraftService(
                    connector.Object,
                    NullLogger<DraftService>.Instance);

            var result =
                await service.GetByDocEntryAsync(
                    5137,
                    null);

            Assert.Null(result);

            connector.Verify(
                x => x.GetDraftAsync(
                    It.IsAny<UserSession>(),
                    It.IsAny<int>()),
                Times.Never);
        }

        // =====================================================
        // UT-053
        // Consultar Draft con sesion
        // =====================================================
        [Fact]
        public async Task GetByDocEntry_WithSession_ReturnsDraft()
        {
            var connector =
                new Mock<ISapDiApiConnector>();

            var expected =
                new DraftDto
                {
                    DocEntry = 5137,
                    CardCode = "P000009",
                    CardName = "Proveedor QA",
                    Confirmed = "tYES"
                };

            connector
                .Setup(x => x.GetDraftAsync(
                    It.IsAny<UserSession>(),
                    5137))
                .ReturnsAsync(expected);

            var service =
                new DraftService(
                    connector.Object,
                    NullLogger<DraftService>.Instance);

            var result =
                await service.GetByDocEntryAsync(
                    5137,
                    CreateSession());

            Assert.NotNull(result);
            Assert.Equal(5137, result.DocEntry);
            Assert.Equal("P000009", result.CardCode);
            Assert.Equal("Proveedor QA", result.CardName);

            connector.Verify(
                x => x.GetDraftAsync(
                    It.IsAny<UserSession>(),
                    5137),
                Times.Once);
        }

        // =====================================================
        // UT-054
        // Filtrar sin sesion
        // =====================================================
        [Fact]
        public async Task GetFiltered_WithoutSession_ReturnsEmpty()
        {
            var connector =
                new Mock<ISapDiApiConnector>();

            var service =
                new DraftService(
                    connector.Object,
                    NullLogger<DraftService>.Instance);

            var filter =
                new DraftFilterDto();

            var result =
                await service.GetFilteredAsync(
                    filter,
                    null);

            Assert.NotNull(result);
            Assert.Empty(result);

            connector.Verify(
                x => x.GetDraftsFilteredAsync(
                    It.IsAny<UserSession>(),
                    It.IsAny<DraftFilterDto>()),
                Times.Never);
        }

        // =====================================================
        // UT-055
        // Filtrar con sesion
        // =====================================================
        [Fact]
        public async Task GetFiltered_WithSession_ReturnsDrafts()
        {
            var connector =
                new Mock<ISapDiApiConnector>();

            var expected =
                new List<DraftDto>
                {
                    new()
                    {
                        DocEntry = 5137,
                        CardCode = "P000009"
                    },
                    new()
                    {
                        DocEntry = 5138,
                        CardCode = "P000010"
                    }
                };

            connector
                .Setup(x => x.GetDraftsFilteredAsync(
                    It.IsAny<UserSession>(),
                    It.IsAny<DraftFilterDto>()))
                .ReturnsAsync(expected);

            var service =
                new DraftService(
                    connector.Object,
                    NullLogger<DraftService>.Instance);

            var result =
                await service.GetFilteredAsync(
                    new DraftFilterDto(),
                    CreateSession());

            var list = result.ToList();

            Assert.Equal(2, list.Count);
            Assert.Equal(5137, list[0].DocEntry);
            Assert.Equal(5138, list[1].DocEntry);
        }

        // =====================================================
        // UT-056
        // Convertir Draft sin sesion
        // =====================================================
        [Fact]
        public async Task SaveDraftToDocument_WithoutSession_ReturnsFalse()
        {
            var connector =
                new Mock<ISapDiApiConnector>();

            var service =
                new DraftService(
                    connector.Object,
                    NullLogger<DraftService>.Instance);

            var result =
                await service.SaveDraftToDocumentAsync(
                    5137,
                    null);

            Assert.False(result.Success);
            Assert.Equal(5137, result.DocEntry);
            Assert.Null(result.GeneratedDocEntry);
            Assert.NotNull(result.ErrorMessage);

            connector.Verify(
                x => x.SaveDraftToDocumentAsync(
                    It.IsAny<UserSession>(),
                    It.IsAny<int>()),
                Times.Never);
        }

        // =====================================================
        // UT-057
        // Convertir Draft correctamente
        // =====================================================
        [Fact]
        public async Task SaveDraftToDocument_WithSession_ReturnsSuccess()
        {
            var connector =
                new Mock<ISapDiApiConnector>();

            connector
                .Setup(x => x.SaveDraftToDocumentAsync(
                    It.IsAny<UserSession>(),
                    5137))
                .ReturnsAsync((
                    true,
                    5137,
                    (int?)9999,
                    (string?)null));

            var service =
                new DraftService(
                    connector.Object,
                    NullLogger<DraftService>.Instance);

            var result =
                await service.SaveDraftToDocumentAsync(
                    5137,
                    CreateSession());

            Assert.True(result.Success);
            Assert.Equal(5137, result.DocEntry);
            Assert.Equal(9999, result.GeneratedDocEntry);
            Assert.Null(result.ErrorMessage);

            connector.Verify(
                x => x.SaveDraftToDocumentAsync(
                    It.IsAny<UserSession>(),
                    5137),
                Times.Once);
        }

        // =====================================================
        // UT-058
        // Error controlado al convertir Draft
        // =====================================================
        [Fact]
        public async Task SaveDraftToDocument_WhenConnectorFails_ReturnsError()
        {
            var connector =
                new Mock<ISapDiApiConnector>();

            connector
                .Setup(x => x.SaveDraftToDocumentAsync(
                    It.IsAny<UserSession>(),
                    5137))
                .ReturnsAsync((
                    false,
                    5137,
                    (int?)null,
                    "No fue posible convertir el borrador"));

            var service =
                new DraftService(
                    connector.Object,
                    NullLogger<DraftService>.Instance);

            var result =
                await service.SaveDraftToDocumentAsync(
                    5137,
                    CreateSession());

            Assert.False(result.Success);
            Assert.Equal(5137, result.DocEntry);
            Assert.Null(result.GeneratedDocEntry);

            Assert.Equal(
                "No fue posible convertir el borrador",
                result.ErrorMessage);
        }

        // =====================================================
        // UT-059
        // Queryable actual
        // =====================================================
        [Fact]
        public void GetDraftsQueryable_CurrentImplementation_ReturnsEmpty()
        {
            var connector =
                new Mock<ISapDiApiConnector>();

            var service =
                new DraftService(
                    connector.Object,
                    NullLogger<DraftService>.Instance);

            var result =
                service.GetDraftsQueryable();

            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}