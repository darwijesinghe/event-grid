using App.API.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace App.API.Data
{
    /// <summary>
    /// Entity Framework Core context that maps the <see cref="Order"/> entity to Azure SQL.
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppDbContext"/> class.
        /// </summary>
        /// <param name="options">EF Core options that include the SQL Server connection.</param>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        /// <summary>
        /// Orders table used by the SQL repository.
        /// </summary>
        public DbSet<Order> Orders { get; set; }

        /// <summary>
        /// Configures table name, key, required fields, and decimal precision for orders.
        /// </summary>
        /// <param name="modelBuilder">EF Core model builder supplied during context initialization.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Keep schema constraints in the model so migrations and runtime mapping stay aligned.
            modelBuilder.Entity<Order>(e =>
            {
                e.ToTable("Orders");
                e.HasKey(x => x.Id);
                e.Property(x => x.CustomerId).HasMaxLength(64).IsRequired();
                e.Property(x => x.Status).HasMaxLength(32).IsRequired();
                e.Property(x => x.Total).HasPrecision(18, 2);
            });
        }
    }
}
