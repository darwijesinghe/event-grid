namespace App.API.Services.Interfaces
{
    /// <summary>
    /// Publishes order lifecycle events to the custom Event Grid topic.
    /// </summary>
    public interface IOrderEventPublisher
    {
        /// <summary>
        /// Sends one order event using either Event Grid or CloudEvents schema.
        /// </summary>
        /// <param name="eventType">Domain event type, such as <c>Order.Created</c>.</param>
        /// <param name="subject">Event subject, typically <c>/orders/{id}</c>.</param>
        /// <param name="data">JSON-serializable payload sent as event data.</param>
        /// <param name="ct">Token used to cancel the publish call.</param>
        /// <returns>
        /// <see langword="true"/> when Event Grid accepts the event; otherwise <see langword="false"/>.
        /// </returns>
        Task<bool> PublishAsync(string eventType, string subject, object data, CancellationToken ct = default);
    }
}
