using App.API.Models;
using App.API.Services.Interfaces;
using Azure;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Options;

namespace App.API.Services
{
    /// <summary>
    /// Publishes order lifecycle events to a custom Azure Event Grid topic.
    /// </summary>
    public sealed class OrderEventPublisher : IOrderEventPublisher
    {
        private readonly ILogger<OrderEventPublisher> _logger;
        private readonly EventGridPublisherClient _client;
        private readonly EventGridOptions         _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderEventPublisher"/> class.
        /// </summary>
        /// <param name="logger">Logger used when publishing fails.</param>
        /// <param name="options">Application settings that include Event Grid topic credentials.</param>
        public OrderEventPublisher(ILogger<OrderEventPublisher> logger, IOptions<AppSettings> options)
        {
            _logger  = logger;
            _options = options.Value.EventGridOptions;
            _client  = new EventGridPublisherClient(new Uri(_options.TopicEndpoint), new AzureKeyCredential(_options.TopicKey));
        }

        /// <summary>
        /// Sends one event using CloudEvents or Event Grid schema based on configuration.
        /// </summary>
        /// <param name="eventType">Domain event type, such as <c>Order.Created</c>.</param>
        /// <param name="subject">Event subject, typically <c>/orders/{id}</c>.</param>
        /// <param name="data">JSON-serializable payload sent as event data.</param>
        /// <param name="ct">Token used to cancel the publish call.</param>
        /// <returns>
        /// <see langword="true"/> when Event Grid returns HTTP 200 or 201; otherwise <see langword="false"/>.
        /// </returns>
        public async Task<bool> PublishAsync(string eventType, string subject, object data, CancellationToken ct = default)
        {
            try
            {
                // CloudEvents is required when the topic or subscription is configured for that schema
                if (_options.UseCloudEvents)
                {
                    var cloudEvent = new CloudEvent("/orderpulse/order-api", eventType, data)
                    {
                        Subject = subject
                    };

                    var clResponse = await _client.SendEventAsync(cloudEvent, ct);
                    if (clResponse.Status != 200 && clResponse.Status != 201)
                    {
                        return false;
                    }

                    return true;
                }

                var response = await _client.SendEventAsync(new EventGridEvent(subject, eventType, "1.0", data), ct);
                if (response.Status != 200 && response.Status != 201)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                // Controllers still return success after a local persist; a false result is logged here only
                _logger.LogError(ex, nameof(PublishAsync));
                return false;
            }
        }
    }
}
