using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace money_be.Controllers
{
    [ApiController]
    [Route("webhooks/nowpayments")]
    public sealed class NowPaymentsWebhookController : ControllerBase
    {
        private readonly string _ipnSecret;
        public NowPaymentsWebhookController(IConfiguration cfg)
        {
            _ipnSecret = cfg["NowPayments:IpnSecret"] ?? throw new InvalidOperationException("NOWPayments IpnSecret missing");
        }

        [HttpPost]
        public async Task<IActionResult> Receive()
        {
            var sigHeader = Request.Headers["x-nowpayments-sig"].ToString();
            var raw = await new StreamReader(Request.Body, Encoding.UTF8).ReadToEndAsync();

            // Parse and re-serialize with keys sorted alphabetically
            using var doc = JsonDocument.Parse(raw);
            var sorted = SortJson(doc.RootElement);
            var message = JsonSerializer.Serialize(sorted);

            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_ipnSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            var computed = Convert.ToHexString(hash).ToLowerInvariant();

            if (!string.Equals(computed, sigHeader, StringComparison.OrdinalIgnoreCase))
                return Unauthorized("Invalid signature");

            // TODO: read payment_status, payment_id, etc., and persist/update your donation record.
            // var status = doc.RootElement.GetProperty("payment_status").GetString();
            return Ok();
        }

        private static object? SortJson(JsonElement el)
        {
            return el.ValueKind switch
            {
                JsonValueKind.Object => el.EnumerateObject()
                                          .OrderBy(p => p.Name, StringComparer.Ordinal)
                                          .ToDictionary(p => p.Name, p => SortJson(p.Value)),
                JsonValueKind.Array => el.EnumerateArray().Select(SortJson).ToArray(),
                JsonValueKind.String => el.GetString(),
                JsonValueKind.Number => el.TryGetInt64(out var i) ? i : el.GetDecimal(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => null
            };
        }
    }
}
