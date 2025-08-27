using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace money_be.Models
{

    public class Donation
    {
        public Guid Id { get; set; }
        [Required, EmailAddress]
        public string Email { get; set; } = null!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        [Range(1, int.MaxValue)]
        public int AmountMinor { get; set; }
        public Currency Currency { get; set; } = Currency.USD;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}