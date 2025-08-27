namespace money_be.DTOs
{
    public sealed class CreateDonationRequest
    {
        public string Email { get; set; } = null!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int AmountMinor { get; set; }
        public string Currency { get; set; } = "USD";
    }
}
