using App.API.Data.Models;
using App.API.Models;
using App.API.Services.Interfaces;
using App.Shared.Events;
using Microsoft.AspNetCore.Mvc;

namespace App.API.Controllers
{
    /// <summary>
    /// HTTP API for creating, paying, cancelling, and retrieving orders.
    /// </summary>
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly ILogger<OrdersController> _logger;
        private readonly IOrderRepository          _orders;
        private readonly IOrderEventPublisher      _publisher;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrdersController"/> class.
        /// </summary>
        /// <param name="logger">Logger used when an action fails.</param>
        /// <param name="orders">Repository used to persist order state.</param>
        /// <param name="publisher">Publisher that sends order events to Event Grid.</param>
        public OrdersController(ILogger<OrdersController> logger, IOrderRepository orders, IOrderEventPublisher publisher)
        {
            _logger    = logger;
            _orders    = orders;
            _publisher = publisher;
        }

        /// <summary>
        /// Creates an order, persists it, then publishes <c>Order.Created</c>.
        /// </summary>
        /// <param name="req">Customer identifier and order total.</param>
        /// <param name="ct">Token used to cancel the request.</param>
        /// <returns>
        /// HTTP 201 with the created order, or HTTP 500 on failure.
        /// </returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderRequest req, CancellationToken ct)
        {
            try
            {
                var order = new Order
                {
                    Id           = Guid.NewGuid(),
                    CustomerId   = req.CustomerId,
                    Total        = req.Total,
                    Status       = "Created",
                    CreatedAtUtc = DateTimeOffset.UtcNow
                };

                // Persist first so subscribers can look up the order after the event is delivered
                await _orders.AddAsync(order, ct);

                await _publisher.PublishAsync(
                    OrderEventTypes.Created,
                    subject: $"/orders/{order.Id}",
                    data   : new OrderCreatedPayload(order.Id, order.CustomerId, order.Total, order.CreatedAtUtc),
                    ct);

                return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(Create));
                return StatusCode(500, "An error occurred while executing the request.");
            }
        }

        /// <summary>
        /// Marks an existing order as paid and publishes <c>Order.Paid</c>.
        /// </summary>
        /// <param name="id">Identifier of the order to pay.</param>
        /// <param name="req">Payment reference included in the event payload.</param>
        /// <param name="ct">Token used to cancel the request.</param>
        /// <returns>
        /// HTTP 200 with the updated order, HTTP 404 when missing, or HTTP 500 on failure.
        /// </returns>
        [HttpPost("{id:guid}/pay")]
        public async Task<IActionResult> Pay(Guid id, [FromBody] PayOrderRequest req, CancellationToken ct)
        {
            try
            {
                var order = await _orders.GetAsync(id, ct);
                if (order is null)
                    return NotFound();

                order.Status = "Paid";
                await _orders.UpdateAsync(order, ct);

                await _publisher.PublishAsync(
                    OrderEventTypes.Paid,
                    $"/orders/{id}",
                    new OrderPaidPayload(id, req.PaymentRef, DateTimeOffset.UtcNow),
                    ct);

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(Pay));
                return StatusCode(500, "An error occurred while executing the request.");
            }
        }

        /// <summary>
        /// Marks an existing order as cancelled and publishes <c>Order.Cancelled</c>.
        /// </summary>
        /// <param name="id">Identifier of the order to cancel.</param>
        /// <param name="req">Cancellation reason included in the event payload.</param>
        /// <param name="ct">Token used to cancel the request.</param>
        /// <returns>
        /// HTTP 200 with the updated order, HTTP 404 when missing, or HTTP 500 on failure.
        /// </returns>
        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelOrderRequest req, CancellationToken ct)
        {
            try
            {
                var order = await _orders.GetAsync(id, ct);
                if (order is null) return NotFound();

                order.Status = "Cancelled";
                await _orders.UpdateAsync(order, ct);

                await _publisher.PublishAsync(
                    OrderEventTypes.Cancelled,
                    $"/orders/{id}",
                    new OrderCancelledPayload(id, req.Reason, DateTimeOffset.UtcNow),
                    ct);

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(Cancel));
                return StatusCode(500, "An error occurred while executing the request.");
            }
        }

        /// <summary>
        /// Returns a single order by identifier.
        /// </summary>
        /// <param name="id">Identifier of the order to load.</param>
        /// <param name="ct">Token used to cancel the request.</param>
        /// <returns>
        /// HTTP 200 with the order, HTTP 404 when missing, or HTTP 500 on failure.
        /// </returns>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            try
            {
                var order = await _orders.GetAsync(id, ct);
                return order is null ? NotFound() : Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(Get));
                return StatusCode(500, "An error occurred while executing the request.");
            }
        }
    }
}
