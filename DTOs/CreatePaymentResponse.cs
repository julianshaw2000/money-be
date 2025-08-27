namespace money_be.DTOs
{
    public sealed class CreatePaymentResponse
    {
        public string PaymentId { get; set; } = string.Empty;
        public string PayAddress { get; set; } = string.Empty;
        public string PayCurrency { get; set; } = string.Empty;
        public decimal PayAmount { get; set; }
        public string? InvoiceUrl { get; set; }
    }
}
