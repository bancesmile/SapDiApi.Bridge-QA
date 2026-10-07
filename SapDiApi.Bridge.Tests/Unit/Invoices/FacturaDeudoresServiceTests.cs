using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SapDiApi.Bridge.Infrastructure.Security;
using SapDiApi.Bridge.Models.Invoices;
using SapDiApi.Bridge.Services.Invoices;
using SapDiApi.Bridge.Services.Sap;
using Xunit;

namespace SapDiApi.Bridge.Tests.Unit.Invoices
{
    public class FacturaDeudoresServiceTests
    {
        private static ApiClientConfig CreateSession()
        {
            return new ApiClientConfig
            {
                Id = "qa-tests",
                Name = "QA Tests",
                ApiKey = "QA-TEST-KEY",
                IsActive = true,
                AllowedCompanies = new List<string> { "*" }
            };
        }

        private static FacturaDeudoresDto CreateInvoice()
        {
            return new FacturaDeudoresDto
            {
                U_Nit = "1234567",
                U_Nombre = "Cliente QA",
                DocCurrency = "QTZ",
                Series = 99,
                TaxDate = DateTime.Today
            };
        }

        // =====================================================
        // UT-029
        // No permitir factura sin sesion/API Key
        // =====================================================
        [Fact]
        public async Task CrearFactura_WithoutSession_ReturnsFalse()
        {
            var connector =
                new Mock<ISapDiApiConnectorFacturas>();

            var service =
                new FacturaDeudoresService(
                    connector.Object,
                    NullLogger<FacturaDeudoresService>.Instance);

            var dto = CreateInvoice();

            var result =
                await service.CrearFacturaDeudoresAsync(
                    dto,
                    null,
                    "SBODEMO_TEST");

            Assert.False(result.success);
            Assert.Null(result.docEntry);
            Assert.Null(result.docNum);

            connector.Verify(
                x => x.ObtenerClientePorNit(
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);

            connector.Verify(
                x => x.CrearFacturaDeudoresSap(
                    It.IsAny<ApiClientConfig>(),
                    It.IsAny<FacturaDeudoresDto>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        // =====================================================
        // UT-030
        // Cliente no encontrado por NIT
        // =====================================================
        [Fact]
        public async Task CrearFactura_WhenCustomerNotFound_ReturnsFalse()
        {
            var connector =
                new Mock<ISapDiApiConnectorFacturas>();

            connector
                .Setup(x => x.ObtenerClientePorNit(
                    "SBODEMO_TEST",
                    "1234567"))
                .ReturnsAsync((
                    false,
                    "Cliente no encontrado",
                    (string?)null,
                    (string?)null));

            var service =
                new FacturaDeudoresService(
                    connector.Object,
                    NullLogger<FacturaDeudoresService>.Instance);

            var result =
                await service.CrearFacturaDeudoresAsync(
                    CreateInvoice(),
                    CreateSession(),
                    "SBODEMO_TEST");

            Assert.False(result.success);

            Assert.Contains(
                "Cliente no encontrado",
                result.message);

            connector.Verify(
                x => x.CrearFacturaDeudoresSap(
                    It.IsAny<ApiClientConfig>(),
                    It.IsAny<FacturaDeudoresDto>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        // =====================================================
        // UT-031
        // Cliente encontrado correctamente
        // =====================================================
        [Fact]
        public async Task CrearFactura_WhenCustomerExists_SendsCustomerToConnector()
        {
            var connector =
                new Mock<ISapDiApiConnectorFacturas>();

            connector
                .Setup(x => x.ObtenerClientePorNit(
                    "SBODEMO_TEST",
                    "1234567"))
                .ReturnsAsync((
                    true,
                    "OK",
                    "C00001",
                    "Cliente QA"));

            connector
                .Setup(x => x.CrearFacturaDeudoresSap(
                    It.IsAny<ApiClientConfig>(),
                    It.IsAny<FacturaDeudoresDto>(),
                    "SBODEMO_TEST",
                    "C00001",
                    "Cliente QA"))
                .ReturnsAsync((
                    true,
                    "OK",
                    (int?)100,
                    (int?)200,
                    "C00001",
                    "1234567",
                    "Cliente QA"));

            var service =
                new FacturaDeudoresService(
                    connector.Object,
                    NullLogger<FacturaDeudoresService>.Instance);

            var result =
                await service.CrearFacturaDeudoresAsync(
                    CreateInvoice(),
                    CreateSession(),
                    "SBODEMO_TEST");

            Assert.True(result.success);

            connector.Verify(
                x => x.CrearFacturaDeudoresSap(
                    It.IsAny<ApiClientConfig>(),
                    It.IsAny<FacturaDeudoresDto>(),
                    "SBODEMO_TEST",
                    "C00001",
                    "Cliente QA"),
                Times.Once);
        }

        // =====================================================
        // UT-032
        // SAP rechaza creacion
        // =====================================================
        [Fact]
        public async Task CrearFactura_WhenSapRejectsInvoice_ReturnsFalse()
        {
            var connector =
                new Mock<ISapDiApiConnectorFacturas>();

            connector
                .Setup(x => x.ObtenerClientePorNit(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync((
                    true,
                    "OK",
                    "C00001",
                    "Cliente QA"));

            connector
                .Setup(x => x.CrearFacturaDeudoresSap(
                    It.IsAny<ApiClientConfig>(),
                    It.IsAny<FacturaDeudoresDto>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync((
                    false,
                    "Error SAP",
                    (int?)null,
                    (int?)null,
                    "C00001",
                    "1234567",
                    "Cliente QA"));

            var service =
                new FacturaDeudoresService(
                    connector.Object,
                    NullLogger<FacturaDeudoresService>.Instance);

            var result =
                await service.CrearFacturaDeudoresAsync(
                    CreateInvoice(),
                    CreateSession(),
                    "SBODEMO_TEST");

            Assert.False(result.success);
            Assert.Contains("Error SAP", result.message);
            Assert.Null(result.docEntry);
            Assert.Null(result.docNum);
        }

        // =====================================================
        // UT-033
        // Factura creada correctamente
        // =====================================================
        [Fact]
        public async Task CrearFactura_WithValidData_ReturnsSuccess()
        {
            var connector =
                new Mock<ISapDiApiConnectorFacturas>();

            connector
                .Setup(x => x.ObtenerClientePorNit(
                    "SBODEMO_TEST",
                    "1234567"))
                .ReturnsAsync((
                    true,
                    "OK",
                    "C00001",
                    "Cliente QA"));

            connector
                .Setup(x => x.CrearFacturaDeudoresSap(
                    It.IsAny<ApiClientConfig>(),
                    It.IsAny<FacturaDeudoresDto>(),
                    "SBODEMO_TEST",
                    "C00001",
                    "Cliente QA"))
                .ReturnsAsync((
                    true,
                    "Factura creada",
                    (int?)123,
                    (int?)456,
                    "C00001",
                    "1234567",
                    "Cliente QA"));

            var service =
                new FacturaDeudoresService(
                    connector.Object,
                    NullLogger<FacturaDeudoresService>.Instance);

            var result =
                await service.CrearFacturaDeudoresAsync(
                    CreateInvoice(),
                    CreateSession(),
                    "SBODEMO_TEST");

            Assert.True(result.success);
            Assert.Equal(123, result.docEntry);
            Assert.Equal(456, result.docNum);
            Assert.Equal("C00001", result.cardCode);
            Assert.Equal("1234567", result.nit);
            Assert.Equal("Cliente QA", result.CardName);
        }

        // =====================================================
        // UT-034
        // Excepcion inesperada controlada
        // =====================================================
        [Fact]
        public async Task CrearFactura_WhenConnectorThrows_ReturnsControlledError()
        {
            var connector =
                new Mock<ISapDiApiConnectorFacturas>();

            connector
                .Setup(x => x.ObtenerClientePorNit(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ThrowsAsync(
                    new Exception("SAP no disponible"));

            var service =
                new FacturaDeudoresService(
                    connector.Object,
                    NullLogger<FacturaDeudoresService>.Instance);

            var result =
                await service.CrearFacturaDeudoresAsync(
                    CreateInvoice(),
                    CreateSession(),
                    "SBODEMO_TEST");

            Assert.False(result.success);

            Assert.Contains(
                "SAP no disponible",
                result.message);

            Assert.Null(result.docEntry);
            Assert.Null(result.docNum);
        }

        // =====================================================
        // UT-035
        // NIT vacio
        // =====================================================
        [Fact]
        public async Task CrearFactura_WithEmptyNit_SendsEmptyNitToConnector()
        {
            var connector =
                new Mock<ISapDiApiConnectorFacturas>();

            connector
                .Setup(x => x.ObtenerClientePorNit(
                    "SBODEMO_TEST",
                    string.Empty))
                .ReturnsAsync((
                    false,
                    "NIT requerido",
                    (string?)null,
                    (string?)null));

            var dto = CreateInvoice();
            dto.U_Nit = string.Empty;

            var service =
                new FacturaDeudoresService(
                    connector.Object,
                    NullLogger<FacturaDeudoresService>.Instance);

            var result =
                await service.CrearFacturaDeudoresAsync(
                    dto,
                    CreateSession(),
                    "SBODEMO_TEST");

            Assert.False(result.success);

            connector.Verify(
                x => x.ObtenerClientePorNit(
                    "SBODEMO_TEST",
                    string.Empty),
                Times.Once);
        }
    }
}