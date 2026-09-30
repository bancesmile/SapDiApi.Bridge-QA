using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SapDiApi.Bridge.Infrastructure.Security;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.Common;
using SapDiApi.Bridge.Models.Invoices;
using SapDiApi.Bridge.Services.ApprovalRequests;
using SapDiApi.Bridge.Services.Invoices;

namespace SapDiApi.Bridge.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [ApiKeyAuth]

    public class InvoicesController : Controller
    {
        private readonly IApprovalRequestService _approvalService;
        private readonly ILogger<InvoicesController> _logger;
        private readonly IFacturaDeudorService _facturaService;
        public InvoicesController(
            IApprovalRequestService approvalService,
            ILogger<InvoicesController> logger,
            IFacturaDeudorService facturaService)
        {
            _approvalService = approvalService;
            _logger = logger;
            _facturaService = facturaService;
        }


        /// <summary>
        /// Crear Factura de deudores (Invoice) en SAP Business One
        /// </summary>
        /// <remarks>
        /// Crea una nueva factura de deudores (Invoice) en SAP Business One.
        /// Compatible directamente con SAP Service Layer: `POST /b1s/v1/Invoices`.
        /// </remarks>
        /// <response code="200">Factura creada exitosamente.</response>
        /// <response code="401">No autorizado - Sesión B1SESSION requerida.</response>
        /// 
        [HttpPost]
        [Route("~/api/v1/Invoices")]
        [ProducesResponseType(typeof(ApiResponse<FacturaDeudoresDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Crear(
            [FromBody] FacturaDeudoresDto request)
        {
            var session = HttpContext.Items["ApiClient"] as ApiClientConfig;
            //var session = HttpContext.Items["ApiKey"] as UserSession;
            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "La información de la factura es requerida."
                });
            }

            if (string.IsNullOrWhiteSpace(request.CardCode))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "El CardCode es requerido."
                });
            }

            if (request.DocumentLines == null ||
                request.DocumentLines.Count == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Debe enviar al menos una línea."
                });
            }

            var resultado =
                await _facturaService.CrearFacturaDeudoresAsync(request, session);

            if (!resultado.success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = resultado.message
                });
            }

            return Ok(new
            {
                success = true,
                message = resultado.message,
                docNum = resultado.docNum
            });
        }
    }
}