You are GitHub Copilot working inside the repository "money-be".
Goal: Add NOWPayments crypto donations support with clean .NET Web API patterns (Program.cs hosting, controllers, typed HttpClient, config-bound secrets, and an IPN webhook with signature verification). Produce compile-ready code. Keep namespaces simple (root "money_be" if present; otherwise infer from csproj). Do not touch unrelated files.

Acceptance criteria:
- POST /api/donations/create accepts { email, firstName?, lastName?, amountMinor:int, currency:'USD'|'EUR'|'GBP' } and returns { paymentId, payAddress, payCurrency, payAmount, invoiceUrl? } from NOWPayments’ /payment endpoint.
- GET /api/donations/status/{paymentId} proxies NOWPayments’ /payment/{id}.
- POST /webhooks/nowpayments validates IPN signature (x-nowpayments-sig) using HMAC-SHA512 of a JSON body with keys sorted alphabetically and an IPN secret. On success, return 200 OK (we’ll persist later).
- All secrets live in configuration (appsettings + env vars). Do NOT expose them to the frontend.
- Add permissive CORS for http://localhost:4200 (adjustable) with credentials off.
- Default to sandbox base URL; allow overriding by env.

Tasks:

1) Create config section and strongly-typed access
   - Add to appsettings.json:
     {
       "NowPayments": {
         "BaseUrl": "https://api-sandbox.nowpayments.io/v1",
         "ApiKey": "CHANGE_ME",
         "IpnSecret": "CHANGE_ME",
         "IpnPublicUrl": "https://localhost:5001/webhooks/nowpayments"
       }
     }
   - Support env vars:
     NOWPAYMENTS__BASEURL
     NOWPAYMENTS__APIKEY
     NOWPAYMENTS__IPNSECRET
     NOWPAYMENTS__IPNPUBLICURL

2) Add DTOs
   - File: /DTOs/CreateDonationRequest.cs
     public sealed class CreateDonationRequest {
       public string Email { get; set; } = null!;
       public string? FirstName { get; set; }
       public string? LastName { get; set; }
       public int AmountMinor { get; set; }            // cents/pence
       public string Currency { get; set; } = "USD";   // "USD"|"EUR"|"GBP"
     }
   - File: /DTOs/CreatePaymentResponse.cs
     public sealed class CreatePaymentResponse {
       public string PaymentId { get; set; } = "";
       public string PayAddress { get; set; } = "";
       public string PayCurrency { get; set; } = "";
       public decimal PayAmount { get; set; }
       public string? InvoiceUrl { get; set; }
     }

3) Typed NOWPayments client
   - File: /Services/NowPaymentsClient.cs
     using System.Net.Http;
     using System.Net.Http.Json;
     using Microsoft.Extensions.Configuration;

     namespace money_be.Services {
       public sealed class NowPaymentsClient {
         private readonly HttpClient _http;
         private readonly string _apiKey;
         private readonly string _baseUrl;
         private readonly string _ipnUrl;

         public NowPaymentsClient(HttpClient http, IConfiguration cfg) {
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
           req.Content = JsonContent.Create(new {
             price_amount = priceAmount,
             price_currency = priceCurrency,
             pay_currency = payCurrency,              // null lets NP select on hosted invoice
             order_id = orderId,
             order_description = orderDesc,
             ipn_callback_url = string.IsNullOrWhiteSpace(_ipnUrl) ? null : _ipnUrl,
             is_fee_paid_by_user = true
           });

           using var res = await _http.SendAsync(req, ct);
           res.EnsureSuccessStatusCode();
           var json = await res.Content.ReadFromJsonAsync<System.Text.Json.Nodes.JsonObject>(cancellationToken: ct)!;

           return new CreatePaymentResponse {
             PaymentId  = json["payment_id"]?.ToString() ?? "",
             PayAddress = json["pay_address"]?.ToString() ?? "",
             PayCurrency= json["pay_currency"]?.ToString() ?? "",
             PayAmount  = decimal.Parse(json["pay_amount"]!.ToString()!),
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

4) Donations API controller
   - File: /Controllers/DonationsController.cs
     using Microsoft.AspNetCore.Mvc;
     using money_be.Services;
     using System.ComponentModel.DataAnnotations;

     namespace money_be.Controllers {
       [ApiController]
       [Route("api/[controller]")]
       public sealed class DonationsController : ControllerBase
       {
         private readonly NowPaymentsClient _np;
         public DonationsController(NowPaymentsClient np) => _np = np;

         [HttpPost("create")]
         public async Task<ActionResult<CreatePaymentResponse>> Create([FromBody] CreateDonationRequest req, CancellationToken ct)
         {
           if (string.IsNullOrWhiteSpace(req.Email)) return BadRequest("Email required");
           if (req.AmountMinor <= 0) return BadRequest("AmountMinor must be > 0");

           var orderId = Guid.NewGuid().ToString("N");
           var desc = $"Donation from {req.Email}";
           var payment = await _np.CreatePaymentAsync(req.AmountMinor, req.Currency, payCurrency: null, orderId, desc, ct);
           return Ok(payment);
         }

         [HttpGet("status/{paymentId}")]
         public async Task<ActionResult<object>> Status([FromRoute, Required] string paymentId, CancellationToken ct)
         {
           var s = await _np.GetPaymentStatusAsync(paymentId, ct);
           return Ok(s);
         }
       }
     }

5) Webhook (IPN) controller with signature verification
   - File: /Controllers/NowPaymentsWebhookController.cs
     using Microsoft.AspNetCore.Mvc;
     using System.Security.Cryptography;
     using System.Text;
     using System.Text.Json;

     namespace money_be.Controllers {
       [ApiController]
       [Route("webhooks/nowpayments")]
       public sealed class NowPaymentsWebhookController : ControllerBase
       {
         private readonly string _ipnSecret;
         public NowPaymentsWebhookController(IConfiguration cfg) {
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
           return el.ValueKind switch {
             JsonValueKind.Object => el.EnumerateObject()
                                       .OrderBy(p => p.Name, StringComparer.Ordinal)
                                       .ToDictionary(p => p.Name, p => SortJson(p.Value)),
             JsonValueKind.Array  => el.EnumerateArray().Select(SortJson).ToArray(),
             JsonValueKind.String => el.GetString(),
             JsonValueKind.Number => el.TryGetInt64(out var i) ? i : el.GetDecimal(),
             JsonValueKind.True   => true,
             JsonValueKind.False  => false,
             _ => null
           };
         }
       }
     }

6) Wire up in Program.cs
   - Ensure:
       var builder = WebApplication.CreateBuilder(args);
       builder.Services.AddControllers();
       builder.Services.AddHttpClient<money_be.Services.NowPaymentsClient>();
       // CORS for Angular (adjust origins)
       builder.Services.AddCors(o => o.AddPolicy("ng", p => p
         .WithOrigins("http://localhost:4200")
         .AllowAnyHeader()
         .AllowAnyMethod()));

       var app = builder.Build();
       app.UseCors("ng");
       app.MapControllers();
       app.Run();

   - If Program.cs already exists, merge the above without removing existing middleware you need (Swagger, etc.).

7) Developer UX
   - Add a README section with:
     - Required config keys and sample curl tests:
       curl -X POST https://localhost:5001/api/donations/create ^
         -H "Content-Type: application/json" ^
         -d "{ \"email\":\"test@example.com\", \"amountMinor\": 1200, \"currency\":\"USD\" }"

8) Keep code style consistent and ensure the project builds:
   - dotnet build
   - dotnet run

Produce the final diff as created/updated file list with full file contents for each new/modified file.
