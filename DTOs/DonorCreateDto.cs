using System.ComponentModel.DataAnnotations;
using money_be.Models;

namespace money_be.DTOs
{
    public class DonorCreateDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(320)]
        public string Email { get; set; } = null!;

        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int AmountMinor { get; set; }

        public Currency? Currency { get; set; }
    }
}
