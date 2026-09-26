namespace App.Shared.Events
{
    /// <summary>
    /// Custom Event Grid event type names shared by the publisher and webhook handlers.
    /// </summary>
    public static class OrderEventTypes
    {
        /// <summary>
        /// Raised after a new order is persisted.
        /// </summary>
        public const string Created   = "Order.Created";

        /// <summary>
        /// Raised after an order is marked paid.
        /// </summary>
        public const string Paid      = "Order.Paid";

        /// <summary>
        /// Raised after an order is cancelled.
        /// </summary>
        public const string Cancelled = "Order.Cancelled";
    }
}
