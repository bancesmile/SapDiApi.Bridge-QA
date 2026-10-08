using Microsoft.Extensions.Hosting;
using Moq;
using SapDiApi.Bridge.Services.Health;
using Xunit;

namespace SapDiApi.Bridge.Tests.Unit.Health
{
    public class HealthServiceTests
    {
        private static HealthService CreateService(
            string environmentName = "QA")
        {
            var environment =
                new Mock<IHostEnvironment>();

            environment
                .SetupGet(x => x.EnvironmentName)
                .Returns(environmentName);

            return new HealthService(
                environment.Object);
        }

        // UT-077
        [Fact]
        public void GetHealthStatus_ReturnsHealthyStatus()
        {
            var service = CreateService();

            var result =
                service.GetHealthStatus();

            Assert.NotNull(result);

            Assert.Equal(
                "BridgeSap REST API",
                result.ServiceName);

       Assert.Equal(
    "Healthy",
    result.Status);
        }

        // UT-078
        [Fact]
        public void GetHealthStatus_ReturnsConfiguredEnvironment()
        {
            var service =
                CreateService("QA");

            var result =
                service.GetHealthStatus();

            Assert.Equal(
                "QA",
                result.Environment);
        }

        // UT-079
        [Fact]
        public void GetHealthStatus_ReturnsVersion()
        {
            var service = CreateService();

            var result =
                service.GetHealthStatus();

            Assert.False(
                string.IsNullOrWhiteSpace(
                    result.Version));
        }

        // UT-081
        [Fact]
        public void GetHealthStatus_ReturnsSystemInformation()
        {
            var service = CreateService();

            var result =
                service.GetHealthStatus();

            Assert.NotNull(result.System);

            Assert.False(
                string.IsNullOrWhiteSpace(
                    result.System.OsPlatform));

            Assert.False(
                string.IsNullOrWhiteSpace(
                    result.System.DotNetVersion));

            Assert.True(
                result.System.ProcessorCount >= 1);

            Assert.True(
                result.System.MemoryUsageMb >= 0);
        }
    }
}