using Azure.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace App.Handlers.Controllers
{
    /// <summary>
    /// CloudEvents webhook that completes Event Grid handshake and logs delivered events.
    /// </summary>
    [ApiController]
    [Route("api/events/cloudevents")]
    public class CloudEventsController : ControllerBase
    {
        private readonly ILogger<CloudEventsController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudEventsController"/> class.
        /// </summary>
        /// <param name="logger">Logger used for handshake and delivered CloudEvents.</param>
        public CloudEventsController(ILogger<CloudEventsController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Answers the CloudEvents webhook origin handshake used by Event Grid.
        /// </summary>
        /// <returns>
        /// HTTP 200 with webhook allowed-origin headers, or HTTP 500 on failure.
        /// </returns>
        [HttpOptions]
        public IActionResult Validate()
        {
            try
            {
                // Echo the requested origin, or allow any origin when the header is omitted.
                var origin = Request.Headers["WebHook-Request-Origin"].FirstOrDefault() ?? "*";

                Response.Headers["WebHook-Allowed-Origin"] = origin;
                Response.Headers["WebHook-Allowed-Rate"]   = "120";

                _logger.LogInformation("CloudEvents OPTIONS validation origin={Origin}", origin);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(Validate));
                return StatusCode(500, "An error occurred while executing the request.");
            }
        }

        /// <summary>
        /// Accepts one or more CloudEvents and logs type, subject, and id.
        /// </summary>
        /// <param name="ct">Token used to cancel the request.</param>
        /// <returns>
        /// HTTP 200 after events are parsed, or HTTP 500 on failure.
        /// </returns>
        [HttpPost]
        public async Task<IActionResult> Receive(CancellationToken ct)
        {
            try
            {
                var body = await BinaryData.FromStreamAsync(Request.Body, ct);
                foreach (var ce in CloudEvent.ParseMany(body))
                {
                    _logger.LogInformation(
                        "CloudEvent type={Type} subject={Subject} id={Id}",
                        ce.Type, ce.Subject, ce.Id);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, nameof(Receive));
                return StatusCode(500, "An error occurred while executing the request.");
            }
        }
    }
}
