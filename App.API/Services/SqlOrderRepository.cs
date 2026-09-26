using App.API.Data;
using App.API.Data.Models;
using App.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace App.API.Services
{
    /// <summary>
    /// Azure SQL implementation of <see cref="IOrderRepository"/> using Entity Framework Core.
    /// </summary>
    public class SqlOrderRepository : IOrderRepository
    {
        // Request-scoped EF Core context shared with the current HTTP request
        private readonly AppDbContext _db;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlOrderRepository"/> class.
        /// </summary>
        /// <param name="db">The orders database context.</param>
        public SqlOrderRepository(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Inserts a new order and saves changes to Azure SQL.
        /// </summary>
        /// <param name="order">The order entity to persist.</param>
        /// <param name="ct">Token used to cancel the write.</param>
        /// <returns>
        /// A task that completes when the insert has been committed.
        /// </returns>
        /// <exception cref="DbUpdateException">
        /// Thrown when the insert cannot be committed.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when <paramref name="ct"/> is cancelled.
        /// </exception>
        public async Task AddAsync(Order order, CancellationToken ct = default)
        {
            _db.Orders.Add(order);
            await _db.SaveChangesAsync(ct);
        }

        /// <summary>
        /// Loads the first order that matches the supplied identifier.
        /// </summary>
        /// <param name="id">The order identifier.</param>
        /// <param name="ct">Token used to cancel the read.</param>
        /// <returns>
        /// The matching order, or <see langword="null"/> when it does not exist.
        /// </returns>
        /// <exception cref="OperationCanceledException">
        /// Thrown when <paramref name="ct"/> is cancelled.
        /// </exception>
        public async Task<Order?> GetAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);
        }

        /// <summary>
        /// Marks the order as modified and saves status or field changes.
        /// </summary>
        /// <param name="order">The order entity with updated values.</param>
        /// <param name="ct">Token used to cancel the write.</param>
        /// <returns>
        /// A task that completes when the update has been committed.
        /// </returns>
        /// <exception cref="DbUpdateException">
        /// Thrown when the update cannot be committed.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when <paramref name="ct"/> is cancelled.
        /// </exception>
        public async Task UpdateAsync(Order order, CancellationToken ct = default)
        {
            // Update attaches the instance when it is not already tracked by this context
            _db.Orders.Update(order);
            await _db.SaveChangesAsync(ct);
        }
    }
}
