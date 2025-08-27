using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using money_be.DTOs;

namespace money_be.Services
{
    public sealed class NowPaymentsClient
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly string _ipnUrl;

        public NowPaymentsClient(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _apiKey = cfg["NowPayments:ApiKey"] ?? throw new InvalidOperationException("NOWPayments ApiKey missing");
            _baseUrl = cfg["NowPayments:BaseUrl"] ?? "https://api-sandbox.nowpayments.io/v1";
            _ipnUrl = cfg["NowPayments:IpnPublicUrl"] ?? "";
        }

        public async Task<CreatePaymentResponse> CreatePaymentAsync(
            int amountMinor, string priceCurrency, string? payCurrency, string orderId, string orderDesc, CancellationToken ct = default)
        {
            var priceAmount = amountMinor / 100m;

            using var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/payment");
            req.Headers.Add("x-api-key", _apiKey);
            req.Content = JsonContent.Create(new
            {
                price_amount = priceAmount,
                price_currency = priceCurrency,
                pay_currency = payCurrency,
                order_id = orderId,
                order_description = orderDesc,
                ipn_callback_url = string.IsNullOrWhiteSpace(_ipnUrl) ? null : _ipnUrl,
                is_fee_paid_by_user = true
            });

            using var res = await _http.SendAsync(req, ct);
            res.EnsureSuccessStatusCode();
            var json = await res.Content.ReadFromJsonAsync<System.Text.Json.Nodes.JsonObject>(cancellationToken: ct)!;

            return new CreatePaymentResponse
            {
                PaymentId = json["payment_id"]?.ToString() ?? "",
                PayAddress = json["pay_address"]?.ToString() ?? "",
                PayCurrency = json["pay_currency"]?.ToString() ?? "",
                PayAmount = decimal.Parse(json["pay_amount"]!.ToString()!),
                InvoiceUrl = json["invoice_url"]?.ToString()
            };
        }

        public async Task<System.Text.Json.Nodes.JsonObject?> GetPaymentStatusAsync(string paymentId, CancellationToken ct = default)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/payment/{paymentId}");
            req.Headers.Add("x-api-key", _apiKey);
            using var res = await _http.SendAsync(req, ct);
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadFromJsonAsync<System.Text.Json.Nodes.JsonObject>(cancellationToken: ct);
        }
    }
}
