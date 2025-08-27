using Microsoft.EntityFrameworkCore;
using money_be.Models;

namespace money_be.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Donation> Donations => Set<Donation>();
        public DbSet<Donor> Donors => Set<Donor>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // If you want to keep Donor config, keep it here. Otherwise, remove if not needed.
            modelBuilder.Entity<Donor>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.HasIndex(d => d.Email).IsUnique();
                entity.Property(d => d.Email).IsRequired().HasMaxLength(320);
                entity.Property(d => d.FirstName).HasMaxLength(100);
                entity.Property(d => d.LastName).HasMaxLength(100);
                entity.Property(d => d.AmountMinor).IsRequired();
                entity.Property(d => d.Currency).HasDefaultValue(Currency.GBP);
                entity.Property(d => d.CreatedAt).HasDefaultValueSql("(now() at time zone 'utc')");
            });
        }
    }
}
