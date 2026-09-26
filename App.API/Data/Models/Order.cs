namespace App.API.Data.Models
{
    /// <summary>
    /// Persisted order entity written to Azure SQL and returned by the orders API.
    /// </summary>
    public class Order
    {
        /// <summary>
        /// Unique order identifier generated when the order is created.
        /// </summary>
        public Guid Id                     { get; set; }

        /// <summary>
        /// Customer who owns the order.
        /// </summary>
        public string CustomerId           { get; set; } = "";

        /// <summary>
        /// Order total persisted with two-decimal precision.
        /// </summary>
        public decimal Total               { get; set; }

        /// <summary>
        /// Lifecycle status of the order.
        /// </summary>
        /// <remarks>
        /// Expected values are Created, Paid, or Cancelled.
        /// </remarks>
        public string Status               { get; set; } = "Created"; // Created | Paid | Cancelled

        /// <summary>
        /// UTC timestamp recorded when the order was first created.
        /// </summary>
        public DateTimeOffset CreatedAtUtc { get; set; }
    }
}
