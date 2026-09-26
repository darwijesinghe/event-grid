using App.Shared.Events;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Channels;

namespace App.Handlers.Controllers
{
    /// <summary>
    /// Event Grid webhook endpoints for inventory, notifications, invoices, and retry demonstration.
    /// </summary>
    [ApiController]
    [Route("api/events")]
    public class EventGridController : ControllerBase
    {
        private readonly ILogger<EventGridController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventGridController"/> class.
        /// </summary>
        /// <param name="logger">Logger used for delivered events and handler failures.</param>
        public EventGridController(ILogger<EventGridController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Receives events for the inventory subscription.
        /// </summary>
        /// <param name="ct">Token used to cancel the request.</param>
        /// <returns>
        /// The result of <see cref="HandleAsync"/>.
        /// </returns>
        [HttpPost("inventory")]
        public Task<IActionResult> Inventory(CancellationToken ct) => HandleAsync("inventory", failAfterValidation: false, ct);

        /// <summary>
        /// Receives events for the notifications subscription.
        /// </summary>
        /// <param name="ct">Token used to cancel the request.</param>
        /// <returns>
        /// The result of <see cref="HandleAsync"/>.
        /// </returns>
        [HttpPost("notifications")]
        public Task<IActionResult> Notifications(CancellationToken ct) => HandleAsync("notifications", failAfterValidation: false, ct);

        /// <summary>
        /// Receives events for the high-value-order subscription.
        /// </summary>
        /// <param name="ct">Token used to cancel the request.</param>
        /// <returns>
        /// The result of <see cref="HandleAsync"/>.
        /// </returns>
        [HttpPost("high-value")]
        public Task<IActionResult> HighValue(CancellationToken ct) => HandleAsync("high-value", failAfterValidation: false, ct);

        /// <summary>
        /// Receives Storage BlobCreated events for invoice uploads.
        /// </summary>
        /// <param name="ct">Token used to cancel the request.</param>
        /// <returns>
        /// The result of <see cref="HandleAsync"/>.
        /// </returns>
        [HttpPost("invoices")]
        public Task<IActionResult> Invoices(CancellationToken ct) => HandleAsync("invoices", failAfterValidation: false, ct);

        /// <summary>
        /// Intentionally returns HTTP 500 after subscription validation so Event Grid retry can be demonstrated.
        /// </summary>
        /// <param name="ct">Token used to cancel the request.</param>
        /// <returns>
        /// The result of <see cref="HandleAsync"/>.
        /// </returns>
        [HttpPost("fail")]
        public Task<IActionResult> Fail(CancellationToken ct) => HandleAsync("fail", failAfterValidation: true, ct);

        /// <summary>
        /// Parses Event Grid events, answers subscription handshake, and logs domain or storage payloads.
        /// </summary>
        /// <param name="handler">Logical subscriber name written to logs.</param>
        /// <param name="failAfterValidation">When <see langword="true"/>, returns HTTP 500 after a successful handshake.</param>
        /// <param name="ct">Token used to cancel the request.</param>
        /// <returns>
        /// HTTP 200 with a validation code, HTTP 200 after processing, or HTTP 500 on failure.
        /// </returns>
        private async Task<IActionResult> HandleAsync(string handler, bool failAfterValidation, CancellationToken ct)
        {
            try
            {
                var events = EventGridEvent.ParseMany(await BinaryData.FromStreamAsync(Request.Body, ct));

                foreach (var eg in events)
                {
                    // Event Grid POSTs a validation event when the subscription is created or updated
                    if (eg.TryGetSystemEventData(out object? data) &&
                        data is SubscriptionValidationEventData validation)
                    {
                        return Ok(new { validationResponse = validation.ValidationCode });
                    }

                    // Fail after handshake so the subscription can be created, then retries can be observed
                    if (failAfterValidation)
                        return StatusCode(500);

                    _logger.LogInformation("[{Handler}] {Type} {Subject}", handler, eg.EventType, eg.Subject);

                    switch (eg.EventType)
                    {
                        case OrderEventTypes.Created:
                            var created = eg.Data.ToObjectFromJson<OrderCreatedPayload>();
                            _logger.LogInformation("Reserve stock {OrderId} {Total}", created!.OrderId, created.Total);
                            break;
                        case OrderEventTypes.Paid:
                            var paid = eg.Data.ToObjectFromJson<OrderPaidPayload>();
                            _logger.LogInformation("Paid {OrderId} {Ref}", paid!.OrderId, paid.PaymentRef);
                            break;
                        case OrderEventTypes.Cancelled:
                            var cancelled = eg.Data.ToObjectFromJson<OrderCancelledPayload>();
                            _logger.LogInformation("Cancelled {OrderId} {Reason}", cancelled!.OrderId, cancelled.Reason);
                            break;
                        case "Microsoft.Storage.BlobCreated":
                            _logger.LogInformation("Invoice blob {Subject}", eg.Subject);
                            break;
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(Inventory));
                return StatusCode(500, "An error occurred while executing the request.");
            }
        }
    }
}
