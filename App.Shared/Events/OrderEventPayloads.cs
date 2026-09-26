namespace App.Shared.Events
{
    /// <summary>
    /// Event data published when an order is created.
    /// </summary>
    /// <param name="OrderId">Identifier of the new order.</param>
    /// <param name="CustomerId">Customer who placed the order.</param>
    /// <param name="Total">Order total at creation time.</param>
    /// <param name="CreatedAtUtc">UTC timestamp when the order was created.</param>
    public sealed record OrderCreatedPayload(
    Guid OrderId, string CustomerId, decimal Total, DateTimeOffset CreatedAtUtc);

    /// <summary>
    /// Event data published when an order is paid.
    /// </summary>
    /// <param name="OrderId">Identifier of the paid order.</param>
    /// <param name="PaymentRef">External payment reference supplied by the client.</param>
    /// <param name="PaidAtUtc">UTC timestamp when payment was recorded.</param>
    public sealed record OrderPaidPayload(
        Guid OrderId, string PaymentRef, DateTimeOffset PaidAtUtc);

    /// <summary>
    /// Event data published when an order is cancelled.
    /// </summary>
    /// <param name="OrderId">Identifier of the cancelled order.</param>
    /// <param name="Reason">Cancellation reason supplied by the client.</param>
    /// <param name="CancelledAtUtc">UTC timestamp when cancellation was recorded.</param>
    public sealed record OrderCancelledPayload(
        Guid OrderId, string Reason, DateTimeOffset CancelledAtUtc);
}
