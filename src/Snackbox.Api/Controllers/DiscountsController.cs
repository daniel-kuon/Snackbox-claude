using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;

namespace Snackbox.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiscountsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DiscountsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/discounts
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DiscountDto>>> GetDiscounts([FromQuery] bool? activeOnly = null)
    {
        var query = _context.Discounts.AsQueryable();

        if (activeOnly == true)
        {
            var now = DateTime.UtcNow;
            query = query.Where(d => d.IsActive && d.ValidFrom <= now && d.ValidTo >= now);
        }

        var discounts = await query
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DiscountDto
            {
                Id = d.Id,
                Name = d.Name,
                ValidFrom = d.ValidFrom,
                ValidTo = d.ValidTo,
                MinimumPurchaseAmount = d.MinimumPurchaseAmount,
                Type = d.Type.ToString(),
                Value = d.Value,
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt
            })
            .ToListAsync();

        return Ok(discounts);
    }

    // GET: api/discounts/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<DiscountDto>> GetDiscount(int id)
    {
        var discount = await _context.Discounts.FindAsync(id);

        if (discount == null)
        {
            return NotFound();
        }

        var discountDto = new DiscountDto
        {
            Id = discount.Id,
            Name = discount.Name,
            ValidFrom = discount.ValidFrom,
            ValidTo = discount.ValidTo,
            MinimumPurchaseAmount = discount.MinimumPurchaseAmount,
            Type = discount.Type.ToString(),
            Value = discount.Value,
            IsActive = discount.IsActive,
            CreatedAt = discount.CreatedAt
        };

        return Ok(discountDto);
    }

    // POST: api/discounts
    [HttpPost]
    public async Task<ActionResult<DiscountDto>> CreateDiscount([FromBody] DiscountDto discountDto)
    {
        if (!Enum.TryParse<DiscountType>(discountDto.Type, out var discountType))
        {
            return BadRequest("Invalid discount type. Must be 'FixedAmount' or 'Percentage'.");
        }

        if (discountType == DiscountType.Percentage && (discountDto.Value < 0 || discountDto.Value > 100))
        {
            return BadRequest("Percentage discount must be between 0 and 100.");
        }

        if (discountDto.ValidFrom >= discountDto.ValidTo)
        {
            return BadRequest("ValidFrom must be before ValidTo.");
        }

        var discount = new Discount
        {
            Name = discountDto.Name,
            ValidFrom = discountDto.ValidFrom,
            ValidTo = discountDto.ValidTo,
            MinimumPurchaseAmount = discountDto.MinimumPurchaseAmount,
            Type = discountType,
            Value = discountDto.Value,
            IsActive = discountDto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Discounts.Add(discount);
        await _context.SaveChangesAsync();

        discountDto.Id = discount.Id;
        discountDto.CreatedAt = discount.CreatedAt;

        return CreatedAtAction(nameof(GetDiscount), new { id = discount.Id }, discountDto);
    }

    // PUT: api/discounts/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDiscount(int id, [FromBody] DiscountDto discountDto)
    {
        if (id != discountDto.Id)
        {
            return BadRequest("ID mismatch.");
        }

        var discount = await _context.Discounts.FindAsync(id);
        if (discount == null)
        {
            return NotFound();
        }

        if (!Enum.TryParse<DiscountType>(discountDto.Type, out var discountType))
        {
            return BadRequest("Invalid discount type. Must be 'FixedAmount' or 'Percentage'.");
        }

        if (discountType == DiscountType.Percentage && (discountDto.Value < 0 || discountDto.Value > 100))
        {
            return BadRequest("Percentage discount must be between 0 and 100.");
        }

        if (discountDto.ValidFrom >= discountDto.ValidTo)
        {
            return BadRequest("ValidFrom must be before ValidTo.");
        }

        discount.Name = discountDto.Name;
        discount.ValidFrom = discountDto.ValidFrom;
        discount.ValidTo = discountDto.ValidTo;
        discount.MinimumPurchaseAmount = discountDto.MinimumPurchaseAmount;
        discount.Type = discountType;
        discount.Value = discountDto.Value;
        discount.IsActive = discountDto.IsActive;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/discounts/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDiscount(int id)
    {
        var discount = await _context.Discounts.FindAsync(id);
        if (discount == null)
        {
            return NotFound();
        }

        _context.Discounts.Remove(discount);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
