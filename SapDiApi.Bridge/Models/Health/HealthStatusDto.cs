namespace SapDiApi.Bridge.Models.Health
{
    /// <summary>
    /// Modelo de estado del servicio para monitoreo y health check.
    /// </summary>
    public class HealthStatusDto
    {
        public string ServiceName { get; set; } = "BridgeSap REST API Service";
        public string Status { get; set; } = "Healthy";
        public string Version { get; set; } = "1.0.0";
        public string Environment { get; set; } = "Production";
        public DateTime ServerTimeUtc { get; set; } = DateTime.UtcNow;
        public TimeSpan Uptime { get; set; }
        public SystemInfoDto System { get; set; } = new();
    }

    public class SystemInfoDto
    {
        public string OsPlatform { get; set; } = Environment.OSVersion.ToString();
        public string DotNetVersion { get; set; } = Environment.Version.ToString();
        public int ProcessorCount { get; set; } = Environment.ProcessorCount;
        public long MemoryUsageMb { get; set; } = GC.GetTotalMemory(false) / (1024 * 1024);
    }
}
