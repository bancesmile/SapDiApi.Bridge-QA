using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using SapDiApi.Bridge.Models.Health;

namespace SapDiApi.Bridge.Services.Health
{
    public class HealthService : IHealthService
    {
        private static readonly DateTime StartTimeUtc = DateTime.UtcNow;
        private readonly IHostEnvironment _environment;

        public HealthService(IHostEnvironment environment)
        {
            _environment = environment;
        }

        public HealthStatusDto GetHealthStatus()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version?.ToString() ?? "1.0.0";

            return new HealthStatusDto
            {
                ServiceName = "BridgeSap REST API",
		Status = "Unhealthy",                
                Version = version,
                Environment = _environment.EnvironmentName,
                ServerTimeUtc = DateTime.UtcNow,
                Uptime = DateTime.UtcNow - StartTimeUtc,
                System = new SystemInfoDto
                {
                    OsPlatform = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                    DotNetVersion = Environment.Version.ToString(),
                    ProcessorCount = Environment.ProcessorCount,
                    MemoryUsageMb = Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024)
                }
            };
        }
    }
}
