using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Snackbox.Api.Controllers;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;
using Xunit;

namespace Snackbox.Api.Tests.Controllers;

public class CardsControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CardsController _controller;

    public CardsControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _controller = new CardsController(_context, NullLogger<CardsController>.Instance);
    }

    private async Task<CardDto> SaveOk(int number, string code30, string code50)
    {
        var result = await _controller.Save(number, new SaveCardDto { Code30 = code30, Code50 = code50 });
        return Assert.IsType<CardDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
    }

    private List<PurchaseBarcode> CodesOf(int number) =>
        _context.Users.Include(u => u.Barcodes).Single(u => u.CardNumber == number)
                .Barcodes.OfType<PurchaseBarcode>().ToList();

    [Fact]
    public async Task Save_NewCard_CreatesInactiveKarteUserWithBothCodes()
    {
        var card = await SaveOk(5, "300005", "500005");

        var user = _context.Users.Single(u => u.CardNumber == 5);
        Assert.Equal("Karte 05", user.Username);
        Assert.False(user.IsActive);  // works for buying, claimed later via the kiosk wizard
        Assert.Equal("300005", card.Code30);
        Assert.Equal("500005", card.Code50);
        Assert.Equal(0.30m, CodesOf(5).Single(b => b.Code == "300005").Amount);
        Assert.Equal(0.50m, CodesOf(5).Single(b => b.Code == "500005").Amount);
    }

    [Fact]
    public async Task Save_ScannedAgain_ReplacesTheCodesOnTheSameCard()
    {
        await SaveOk(7, "A30", "A50");

        await SaveOk(7, "B30", "B50");

        Assert.Single(_context.Users.Where(u => u.CardNumber == 7));
        var codes = CodesOf(7);
        Assert.Equal(2, codes.Count);
        Assert.Equal(0.30m, codes.Single(b => b.Code == "B30").Amount);
        Assert.Equal(0.50m, codes.Single(b => b.Code == "B50").Amount);
    }

    [Fact]
    public async Task Save_RescanCorrectingSwappedCodes_MovesTheAmountsInsteadOfTheCodes()
    {
        // First scan had them the wrong way round; the second fixes it. Moving the codes
        // between rows would put one code on two rows and trip the unique index.
        await SaveOk(8, "X", "Y");
        var rowOfX = CodesOf(8).Single(b => b.Code == "X").Id;

        await SaveOk(8, "Y", "X");

        var codes = CodesOf(8);
        Assert.Equal(0.50m, codes.Single(b => b.Code == "X").Amount);
        Assert.Equal(0.30m, codes.Single(b => b.Code == "Y").Amount);
        Assert.Equal(rowOfX, codes.Single(b => b.Code == "X").Id);  // history stays with the code
    }

    [Fact]
    public async Task Save_CodeOfAnotherCard_IsRejected()
    {
        await SaveOk(1, "C30", "C50");

        var result = await _controller.Save(2, new SaveCardDto { Code30 = "C30", Code50 = "D50" });

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.False(_context.Users.Any(u => u.CardNumber == 2));
    }

    [Fact]
    public async Task Save_SameCodeTwice_IsRejected()
    {
        var result = await _controller.Save(3, new SaveCardDto { Code30 = "SAME", Code50 = "SAME" });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Save_ClaimedCard_KeepsItsNumberAndOwner()
    {
        await SaveOk(9, "E30", "E50");
        var user = _context.Users.Single(u => u.CardNumber == 9);
        user.Username = "Jane Doe";  // claimed through the kiosk wizard
        user.IsActive = true;
        await _context.SaveChangesAsync();

        var card = await SaveOk(9, "F30", "F50");

        Assert.Equal("Jane Doe", card.Username);
        Assert.True(card.IsClaimed);
        Assert.False(_context.Users.Any(u => u.Username == "Karte 09"));  // no duplicate card 9
    }

    [Fact]
    public async Task Save_ImportedCardWithCatchAllBarcode_LeavesTheCatchAllAlone()
    {
        // Like imported "Karte 16": a €0.50 code plus the non-scannable catch-all that holds
        // scans of codes deleted years ago. Setting the €0.30 code must not repurpose it.
        var user = new User { Username = "Karte 16", CardNumber = 16, CreatedAt = DateTime.UtcNow };
        user.Barcodes.Add(new PurchaseBarcode { Code = "LEGACY-IMPORT-16", Amount = 0m, CreatedAt = DateTime.UtcNow });
        user.Barcodes.Add(new PurchaseBarcode { Code = "OLD50", Amount = 0.50m, CreatedAt = DateTime.UtcNow });
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await SaveOk(16, "NEW30", "NEW50");

        var codes = CodesOf(16);
        Assert.Equal(0m, codes.Single(b => b.Code == "LEGACY-IMPORT-16").Amount);
        Assert.Equal(0.30m, codes.Single(b => b.Code == "NEW30").Amount);
        Assert.Equal(0.50m, codes.Single(b => b.Code == "NEW50").Amount);
        Assert.Equal(3, codes.Count);
    }

    [Fact]
    public async Task GetRange_ListsEveryNumber_WithExistingCardsFilledIn()
    {
        await SaveOk(11, "G30", "G50");

        var result = await _controller.GetRange(10, 3);
        var cards = Assert.IsType<List<CardDto>>(Assert.IsType<OkObjectResult>(result.Result).Value);

        Assert.Equal(new[] { 10, 11, 12 }, cards.Select(c => c.Number));
        Assert.Null(cards[0].Code30);
        Assert.Equal("G30", cards[1].Code30);
        Assert.Null(cards[2].UserId);
    }

    public void Dispose() => _context.Dispose();
}
