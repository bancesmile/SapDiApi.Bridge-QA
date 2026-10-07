using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SapDiApi.Bridge.Models.ApprovalRequests;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Services.ApprovalRequests;
using SapDiApi.Bridge.Services.Sap;
using Xunit;

namespace SapDiApi.Bridge.Tests.Unit.ApprovalRequests
{
    public class ApprovalRequestServiceTests
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
        // UT-044
        // Consultar sin sesion
        // =====================================================
        [Fact]
        public async Task GetByCode_WithoutSession_ReturnsNull()
        {
            var connector =
                new Mock<ISapDiApiConnector>();

            var service =
                new ApprovalRequestService(
                    connector.Object,
                    NullLogger<ApprovalRequestService>.Instance);

            var result =
                await service.GetByCodeAsync(
                    6106,
                    null);

            Assert.Null(result);

            connector.Verify(
                x => x.GetApprovalRequestAsync(
                    It.IsAny<UserSession>(),
                    It.IsAny<int>()),
                Times.Never);
        }

        // =====================================================
        // UT-045
        // Consultar con sesion
        // =====================================================
        [Fact]
        public async Task GetByCode_WithSession_ReturnsApprovalRequest()
        {
            var connector =
                new Mock<ISapDiApiConnector>();

            var expected =
                new ApprovalRequestDto
                {
                    Code = 6106,
                    Status = "arsApproved",
                    DraftEntry = 5137
                };

            connector
                .Setup(x => x.GetApprovalRequestAsync(
                    It.IsAny<UserSession>(),
                    6106))
                .ReturnsAsync(expected);

            var service =
                new ApprovalRequestService(
                    connector.Object,
                    NullLogger<ApprovalRequestService>.Instance);

            var result =
                await service.GetByCodeAsync(
                    6106,
                    CreateSession());

            Assert.NotNull(result);
            Assert.Equal(6106, result.Code);
            Assert.Equal("arsApproved", result.Status);
            Assert.Equal(5137, result.DraftEntry);

            connector.Verify(
                x => x.GetApprovalRequestAsync(
                    It.IsAny<UserSession>(),
                    6106),
                Times.Once);
        }

        // =====================================================
        // UT-046
        // Filtrar sin sesion
        // =====================================================
        [Fact]
        public async Task GetFiltered_WithoutSession_ReturnsEmpty()
        {
            var connector =
                new Mock<ISapDiApiConnector>();

            var service =
                new ApprovalRequestService(
                    connector.Object,
                    NullLogger<ApprovalRequestService>.Instance);

            var filter =
                new ApprovalRequestFilterDto();

            var result =
                await service.GetFilteredAsync(
                    filter,
                    null);

            Assert.NotNull(result);
            Assert.Empty(result);

            connector.Verify(
                x => x.GetApprovalRequestsFilteredAsync(
                    It.IsAny<UserSession>(),
                    It.IsAny<ApprovalRequestFilterDto>()),
                Times.Never);
        }

        // =====================================================
        // UT-047
        // Filtrar con sesion
        // =====================================================
        [Fact]
        public async Task GetFiltered_WithSession_ReturnsResults()
        {
            var connector =
                new Mock<ISapDiApiConnector>();

            var expected =
                new List<ApprovalRequestDto>
                {
                    new()
                    {
                        Code = 6106,
                        Status = "arsApproved"
                    },
                    new()
                    {
                        Code = 6107,
                        Status = "arsPending"
                    }
                };

            connector
                .Setup(x => x.GetApprovalRequestsFilteredAsync(
                    It.IsAny<UserSession>(),
                    It.IsAny<ApprovalRequestFilterDto>()))
                .ReturnsAsync(expected);

            var service =
                new ApprovalRequestService(
                    connector.Object,
                    NullLogger<ApprovalRequestService>.Instance);

            var result =
                await service.GetFilteredAsync(
                    new ApprovalRequestFilterDto(),
                    CreateSession());

            var list = result.ToList();

            Assert.Equal(2, list.Count);
            Assert.Equal(6106, list[0].Code);
            Assert.Equal(6107, list[1].Code);
        }

        // =====================================================
        // UT-048
        // Actualizar sin sesion
        // =====================================================
        [Fact]
        public async Task Update_WithoutSession_ReturnsFalse()
        {
            var connector =
                new Mock<ISapDiApiConnector>();

            var service =
                new ApprovalRequestService(
                    connector.Object,
                    NullLogger<ApprovalRequestService>.Instance);

            var dto =
                new UpdateApprovalRequestDto();

            var result =
                await service.UpdateAsync(
                    6106,
                    dto,
                    null);

            Assert.False(result.Success);
            Assert.Equal(6106, result.Code);
            Assert.NotNull(result.ErrorMessage);

            connector.Verify(
                x => x.UpdateApprovalRequestAsync(
                    It.IsAny<UserSession>(),
                    It.IsAny<int>(),
                    It.IsAny<UpdateApprovalRequestDto>()),
                Times.Never);
        }

        // =====================================================
        // UT-049
        // Actualizacion correcta
        // =====================================================
        [Fact]
        public async Task Update_WithSession_ReturnsSuccess()
        {
            var connector =
                new Mock<ISapDiApiConnector>();

            connector
                .Setup(x => x.UpdateApprovalRequestAsync(
                    It.IsAny<UserSession>(),
                    6106,
                    It.IsAny<UpdateApprovalRequestDto>()))
                .ReturnsAsync((
                    true,
                    6106,
                    (string?)null));

            var service =
                new ApprovalRequestService(
                    connector.Object,
                    NullLogger<ApprovalRequestService>.Instance);

            var dto =
                new UpdateApprovalRequestDto();

            var result =
                await service.UpdateAsync(
                    6106,
                    dto,
                    CreateSession());

            Assert.True(result.Success);
            Assert.Equal(6106, result.Code);
            Assert.Null(result.ErrorMessage);

            connector.Verify(
                x => x.UpdateApprovalRequestAsync(
                    It.IsAny<UserSession>(),
                    6106,
                    dto),
                Times.Once);
        }

        // =====================================================
        // UT-050
        // Error controlado al actualizar
        // =====================================================
        [Fact]
        public async Task Update_WhenConnectorFails_ReturnsError()
        {
            var connector =
                new Mock<ISapDiApiConnector>();

            connector
                .Setup(x => x.UpdateApprovalRequestAsync(
                    It.IsAny<UserSession>(),
                    6106,
                    It.IsAny<UpdateApprovalRequestDto>()))
                .ReturnsAsync((
                    false,
                    6106,
                    "Solicitud no encontrada"));

            var service =
                new ApprovalRequestService(
                    connector.Object,
                    NullLogger<ApprovalRequestService>.Instance);

            var result =
                await service.UpdateAsync(
                    6106,
                    new UpdateApprovalRequestDto(),
                    CreateSession());

            Assert.False(result.Success);
            Assert.Equal(6106, result.Code);
            Assert.Equal(
                "Solicitud no encontrada",
                result.ErrorMessage);
        }

        // =====================================================
        // UT-051
        // Queryable actual
        // =====================================================
        [Fact]
        public void GetApprovalRequestsQueryable_CurrentImplementation_ReturnsEmpty()
        {
            var connector =
                new Mock<ISapDiApiConnector>();

            var service =
                new ApprovalRequestService(
                    connector.Object,
                    NullLogger<ApprovalRequestService>.Instance);

            var result =
                service.GetApprovalRequestsQueryable();

            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}