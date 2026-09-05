using Microsoft.AspNetCore.Mvc;

namespace SapDiApi.Bridge.Infrastructure.Security
{
    /// <summary>
    /// Exige una sesión activa de SAP Service Layer (B1SESSION o Cookie) para acceder al endpoint.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class B1SessionAuthAttribute : TypeFilterAttribute
    {
        public B1SessionAuthAttribute() : base(typeof(B1SessionAuthFilter))
        {
        }
    }
}
