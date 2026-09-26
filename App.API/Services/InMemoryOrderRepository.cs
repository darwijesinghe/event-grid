using App.API.Data.Models;
using App.API.Services.Interfaces;
using System.Collections.Concurrent;

namespace App.API.Services
{
    /// <summary>
    /// In-memory <see cref="IOrderRepository"/> used for local testing when SQL is not registered.
    /// </summary>
    public sealed class InMemoryOrderRepository : IOrderRepository
    {
        // Process-wide store so orders survive across scoped repository instances.
        private static readonly ConcurrentDictionary<Guid, Order> Store = new();

        /// <summary>
        /// Adds or overwrites an order in the in-memory dictionary.
        /// </summary>
        /// <param name="order">The order entity to persist.</param>
        /// <param name="ct">Unused cancellation token kept for interface compatibility.</param>
        /// <returns>
        /// A completed task after the dictionary write.
        /// </returns>
        public Task AddAsync(Order order, CancellationToken ct = default)
        {
            Store[order.Id] = order;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Attempts to load an order from the in-memory dictionary.
        /// </summary>
        /// <param name="id">The order identifier.</param>
        /// <param name="ct">Unused cancellation token kept for interface compatibility.</param>
        /// <returns>
        /// The matching order, or <see langword="null"/> when it does not exist.
        /// </returns>
        public Task<Order?> GetAsync(Guid id, CancellationToken ct = default)
        {
            Store.TryGetValue(id, out var order);
            return Task.FromResult(order);
        }

        /// <summary>
        /// Replaces the stored order with the supplied instance.
        /// </summary>
        /// <param name="order">The order entity with updated values.</param>
        /// <param name="ct">Unused cancellation token kept for interface compatibility.</param>
        /// <returns>
        /// A completed task after the dictionary write.
        /// </returns>
        public Task UpdateAsync(Order order, CancellationToken ct = default)
        {
            Store[order.Id] = order;
            return Task.CompletedTask;
        }
    }
}
