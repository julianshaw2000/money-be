using Microsoft.AspNetCore.Mvc;
using money_be.Services;
using money_be.DTOs;
using System.ComponentModel.DataAnnotations;

namespace money_be.Controllers
{
    [ApiController]
    [Route("api/donations")]
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