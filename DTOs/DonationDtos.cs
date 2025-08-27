using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using money_be.Models;

namespace money_be.DTOs
{
    // Currency enum is defined in Models; use that only.

    public sealed class DonationCreateDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        [Range(1, int.MaxValue)]
        public int AmountMinor { get; set; }
        [Required]
        public Currency Currency { get; set; } = Currency.USD;
        public DateTime? CreatedAt { get; set; }
    }

    public sealed class DonationReadDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = null!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int AmountMinor { get; set; }
        public Currency Currency { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}