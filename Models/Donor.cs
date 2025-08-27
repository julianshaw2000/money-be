using System;

namespace money_be.Models
{
    public enum Currency
    {
        GBP,
        USD,
        EUR
    }

    public class Donor
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = null!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int AmountMinor { get; set; }
        public Currency Currency { get; set; } = Currency.USD;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
