using Microsoft.AspNetCore.Mvc;

namespace SapDiApi.Bridge.Infrastructure.Security
{
    /// <summary>
    /// Aplica validación estricta de API Key sobre el controlador o acción decorada.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ApiKeyAuthAttribute : TypeFilterAttribute
    {
        public ApiKeyAuthAttribute() : base(typeof(ApiKeyAuthFilter))
        {
        }
    }
}
