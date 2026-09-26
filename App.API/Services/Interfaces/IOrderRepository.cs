using App.API.Data.Models;

namespace App.API.Services.Interfaces
{
    /// <summary>
    /// Persistence contract for creating, reading, and updating orders.
    /// </summary>
    public interface IOrderRepository
    {
        /// <summary>
        /// Inserts a new order into the store.
        /// </summary>
        /// <param name="order">The order entity to persist.</param>
        /// <param name="ct">Token used to cancel the write.</param>
        /// <returns>
        /// A task that completes when the order has been saved.
        /// </returns>
        /// <exception cref="OperationCanceledException">
        /// Thrown when <paramref name="ct"/> is cancelled.
        /// </exception>
        Task AddAsync(Order order, CancellationToken ct = default);

        /// <summary>
        /// Loads an order by identifier.
        /// </summary>
        /// <param name="id">The order identifier.</param>
        /// <param name="ct">Token used to cancel the read.</param>
        /// <returns>
        /// The matching order, or <see langword="null"/> when it does not exist.
        /// </returns>
        /// <exception cref="OperationCanceledException">
        /// Thrown when <paramref name="ct"/> is cancelled.
        /// </exception>
        Task<Order?> GetAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Persists changes to an existing order.
        /// </summary>
        /// <param name="order">The order entity with updated values.</param>
        /// <param name="ct">Token used to cancel the write.</param>
        /// <returns>
        /// A task that completes when the update has been saved.
        /// </returns>
        /// <exception cref="OperationCanceledException">
        /// Thrown when <paramref name="ct"/> is cancelled.
        /// </exception>
        Task UpdateAsync(Order order, CancellationToken ct = default);
    }
}
