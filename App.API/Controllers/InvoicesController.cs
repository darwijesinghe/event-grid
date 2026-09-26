using App.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace App.API.Controllers
{
    /// <summary>
    /// HTTP API for uploading invoice files against an existing order.
    /// </summary>
    [ApiController]
    [Route("api/orders")]
    public class InvoicesController : ControllerBase
    {
        private readonly ILogger<OrdersController> _logger;
        private readonly IOrderRepository          _orders;
        private readonly IInvoiceBlobService       _blobs;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvoicesController"/> class.
        /// </summary>
        /// <param name="logger">Logger used when an upload fails.</param>
        /// <param name="orders">Repository used to confirm the order exists.</param>
        /// <param name="blobs">Service that writes the invoice to Azure Blob Storage.</param>
        public InvoicesController(ILogger<OrdersController> logger, IOrderRepository orders, IInvoiceBlobService blobs)
        {
            _logger = logger;
            _orders = orders;
            _blobs  = blobs;
        }

        /// <summary>
        /// Uploads an invoice for an existing order, which can trigger a Storage BlobCreated system event.
        /// </summary>
        /// <param name="id">Identifier of the order that owns the invoice.</param>
        /// <param name="file">Invoice file posted as multipart form data.</param>
        /// <param name="ct">Token used to cancel the request.</param>
        /// <returns>
        /// HTTP 202 when accepted, HTTP 400 when the file is missing, HTTP 404 when the order is missing, or HTTP 500 on failure.
        /// </returns>
        [HttpPost("{id:guid}/invoice")]
        [RequestSizeLimit(5_000_000)]
        public async Task<IActionResult> UploadInvoice(Guid id, IFormFile file, CancellationToken ct)
        {
            try
            {
                if (file is null || file.Length == 0) 
                    return BadRequest("File is required.");

                // Reject uploads for unknown orders so blobs are not created without a matching row
                if (await _orders.GetAsync(id, ct) is null) 
                    return NotFound();

                await _blobs.UploadAsync(id, file, ct);
                return Accepted(new { orderId = id, fileName = file.FileName });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(UploadInvoice));
                return StatusCode(500, "An error occurred while executing the request.");
            }
        }
    }
}
