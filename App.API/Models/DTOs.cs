namespace App.API.Models
{
    /// <summary>
    /// Request body used to create a new order.
    /// </summary>
    /// <param name="CustomerId">Identifier of the customer placing the order.</param>
    /// <param name="Total">Monetary total of the order.</param>
    public sealed record CreateOrderRequest(string CustomerId, decimal Total);

    /// <summary>
    /// Request body used to mark an order as paid.
    /// </summary>
    /// <param name="PaymentRef">External payment reference recorded with the paid event.</param>
    public sealed record PayOrderRequest(string PaymentRef);

    /// <summary>
    /// Request body used to cancel an order.
    /// </summary>
    /// <param name="Reason">Human-readable reason published with the cancelled event.</param>
    public sealed record CancelOrderRequest(string Reason);
}
