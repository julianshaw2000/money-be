using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using money_be.Data;
using money_be.DTOs;
using money_be.Models;

namespace money_be.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class DonationsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public DonationsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DonationCreateDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var donation = new Donation
            {
                Id = Guid.NewGuid(),
                Email = dto.Email.Trim(),
                FirstName = dto.FirstName?.Trim(),
                LastName = dto.LastName?.Trim(),
                AmountMinor = dto.AmountMinor,
                Currency = dto.Currency,
                CreatedAt = DateTime.UtcNow
            };
            _db.Donations.Add(donation);
            await _db.SaveChangesAsync();

            var readDto = new DonationReadDto
            {
                Id = donation.Id,
                Email = donation.Email,
                FirstName = donation.FirstName,
                LastName = donation.LastName,
                AmountMinor = donation.AmountMinor,
                Currency = donation.Currency,
                CreatedAt = donation.CreatedAt
            };
            return CreatedAtAction(nameof(GetById), new { id = donation.Id }, readDto);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<DonationReadDto>> GetById(Guid id)
        {
            var donation = await _db.Donations.FindAsync(id);
            if (donation == null) return NotFound();
            var readDto = new DonationReadDto
            {
                Id = donation.Id,
                Email = donation.Email,
                FirstName = donation.FirstName,
                LastName = donation.LastName,
                AmountMinor = donation.AmountMinor,
                Currency = donation.Currency,
                CreatedAt = donation.CreatedAt
            };
            return Ok(readDto);
        }
    }
}