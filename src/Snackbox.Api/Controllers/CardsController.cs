using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;

namespace Snackbox.Api.Controllers;

/// <summary>
/// Numbered, pre-made cards. Each has a €0.30 and a €0.50 purchase code and starts out as an
/// inactive user "Karte NN" that works for buying straight away and is claimed later through
/// the kiosk's setup wizard. Used by the admin card wizard to set up a batch of cards.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class CardsController : ControllerBase
{
    private const decimal Amount30 = 0.30m;
    private const decimal Amount50 = 0.50m;

    private readonly ApplicationDbContext _context;
    private readonly ILogger<CardsController> _logger;

    public CardsController(ApplicationDbContext context, ILogger<CardsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>The name an unclaimed card gets - the same "Karte 05" the old Snackbox used.</summary>
    public static string CardName(int number) => $"Karte {number:00}";

    /// <summary>The cards from <paramref name="from"/> on, existing or not, so the wizard can show the whole batch.</summary>
    [HttpGet]
    public async Task<ActionResult<List<CardDto>>> GetRange([FromQuery] int from, [FromQuery] int count)
    {
        if (from < 1 || count < 1 || count > 500)
        {
            return BadRequest(new { message = "Start at card 1 or higher and ask for 1 to 500 cards." });
        }

        var to = from + count - 1;
        var users = await _context.Users
                                  .Include(u => u.Barcodes)
                                  .Where(u => u.CardNumber >= from && u.CardNumber <= to)
                                  .ToListAsync();

        return Ok(Enumerable.Range(from, count)
                            .Select(number => ToDto(number, users.FirstOrDefault(u => u.CardNumber == number)))
                            .ToList());
    }

    /// <summary>
    /// Sets a card's two codes. Creates the card if it does not exist yet; otherwise replaces
    /// its codes in place, so purchases made with the old ones keep their history.
    /// </summary>
    [HttpPut("{number:int}")]
    public async Task<ActionResult<CardDto>> Save(int number, [FromBody] SaveCardDto dto)
    {
        var code30 = dto.Code30.Trim();
        var code50 = dto.Code50.Trim();

        if (number < 1) return BadRequest(new { message = "Card numbers start at 1." });
        if (code30.Length == 0 || code50.Length == 0) return BadRequest(new { message = "Both codes are needed." });
        if (code30 == code50) return BadRequest(new { message = "The €0.30 and the €0.50 code are the same - scan the other one." });

        var user = await _context.Users.Include(u => u.Barcodes).FirstOrDefaultAsync(u => u.CardNumber == number);

        // A code may only belong to this card. Matching our own code is fine - that is the
        // same card scanned again.
        foreach (var code in new[] { code30, code50 })
        {
            var owner = await _context.Barcodes
                                      .Where(b => b.Code == code && (user == null || b.UserId != user.Id))
                                      .Select(b => b.User.Username)
                                      .FirstOrDefaultAsync();
            if (owner != null)
            {
                return Conflict(new { message = $"Code {code} already belongs to {owner}." });
            }
        }

        if (user == null)
        {
            var name = CardName(number);
            if (await _context.Users.AnyAsync(u => u.Username == name))
            {
                return Conflict(new { message = $"There is already a user called {name} without a card number." });
            }

            user = new User
            {
                Username = name,
                IsActive = false,  // works for buying at once; claimed via the kiosk wizard
                CardNumber = number,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
        }

        SetCodes(user, code30, code50);

        // One SaveChanges: a new card and its codes are written together or not at all.
        await _context.SaveChangesAsync();
        _logger.LogInformation("Card {Number} set up with codes {Code30} / {Code50}", number, code30, code50);

        return Ok(ToDto(number, user));
    }

    private static void SetCodes(User user, string code30, string code50)
    {
        var rows = user.Barcodes.OfType<PurchaseBarcode>().ToList();
        var taken = new HashSet<PurchaseBarcode>();
        var stillToPlace = new List<(decimal Amount, string Code)>();

        // A code the card already has keeps its row - and the purchase history on it - and
        // only its amount moves. That also covers a re-scan correcting swapped codes, which
        // would otherwise put one code on two rows for a moment and trip the unique index.
        foreach (var (amount, code) in new[] { (Amount30, code30), (Amount50, code50) })
        {
            var same = rows.FirstOrDefault(b => b.Code == code);
            if (same == null)
            {
                stillToPlace.Add((amount, code));
                continue;
            }

            same.Amount = amount;
            taken.Add(same);
        }

        foreach (var (amount, code) in stillToPlace)
        {
            // Only a row of the same amount is reused. Any other row is left alone - imported
            // cards can carry the non-scannable LEGACY-IMPORT catch-all, which holds old history.
            var row = rows.FirstOrDefault(b => !taken.Contains(b) && b.Amount == amount);
            if (row == null)
            {
                user.Barcodes.Add(new PurchaseBarcode { Code = code, Amount = amount, User = user, CreatedAt = DateTime.UtcNow });
                continue;
            }

            row.Code = code;
            row.Amount = amount;
            taken.Add(row);
        }
    }

    private static CardDto ToDto(int number, User? user) => new()
    {
        Number = number,
        UserId = user?.Id,
        Username = user?.Username,
        IsClaimed = user?.IsActive == true,
        Code30 = user?.Barcodes.OfType<PurchaseBarcode>().FirstOrDefault(b => b.Amount == Amount30)?.Code,
        Code50 = user?.Barcodes.OfType<PurchaseBarcode>().FirstOrDefault(b => b.Amount == Amount50)?.Code
    };
}
