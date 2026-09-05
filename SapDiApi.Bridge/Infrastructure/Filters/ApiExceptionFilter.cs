using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SapDiApi.Bridge.Models.Common;

namespace SapDiApi.Bridge.Infrastructure.Filters
{
    public class ApiExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<ApiExceptionFilter> _logger;
        private readonly IHostEnvironment _environment;

        public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger, IHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception, "Excepción no controlada en solicitud HTTP: {Message}", context.Exception.Message);

            var errorResponse = new ApiErrorResponse
            {
                Success = false,
                Error = "Error interno del servidor",
                Details = _environment.IsDevelopment() ? context.Exception.ToString() : context.Exception.Message,
                StatusCode = StatusCodes.Status500InternalServerError,
                TimestampUtc = DateTime.UtcNow
            };

            context.Result = new ObjectResult(errorResponse)
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };

            context.ExceptionHandled = true;
        }
    }
}
