using SapDiApi.Bridge.Models.Health;

namespace SapDiApi.Bridge.Services.Health
{
    public interface IHealthService
    {
        HealthStatusDto GetHealthStatus();
    }
}
