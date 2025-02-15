using Microsoft.EntityFrameworkCore;

public class ExchangeRateContext : DbContext
{
    public ExchangeRateContext(DbContextOptions<ExchangeRateContext> options)
        : base(options)
    {
    }

    public DbSet<ExchangeRate> ExchangeRates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExchangeRate>()
            .HasIndex(e => new { e.Date, e.CurrencyCode })
            .IsUnique();
    }
}
