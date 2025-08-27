using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using money_be.Data;
using money_be.Models;
using System.Threading.Tasks;

namespace money_be.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DonorsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public DonorsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var donors = await _db.Donors.ToListAsync();
            return Ok(donors);
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] money_be.DTOs.DonorCreateDto dto)
        {
            // Model validation
            if (string.IsNullOrWhiteSpace(dto.Email) || dto.Email.Length > 320)
                return BadRequest(new { error = "Email is required and must be <= 320 chars." });
            if (dto.AmountMinor < 1)
                return BadRequest(new { error = "AmountMinor must be >= 1." });

            // Check unique email
            var exists = await _db.Donors.AnyAsync(d => d.Email == dto.Email);
            if (exists)
                return Conflict(new { error = "Email already exists." });

            var donor = new Donor
            {
                Id = Guid.NewGuid(),
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                AmountMinor = dto.AmountMinor,
                Currency = (Currency)(dto.Currency ?? Currency.GBP),
                CreatedAt = DateTime.UtcNow
            };

            _db.Donors.Add(donor);
            await _db.SaveChangesAsync();

            var result = new
            {
                donor.Id,
                donor.Email,
                donor.FirstName,
                donor.LastName,
                donor.AmountMinor,
                donor.Currency,
                donor.CreatedAt
            };
            var location = $"/api/donors/{donor.Id}";
            return Created(location, result);
        }
    }
}
