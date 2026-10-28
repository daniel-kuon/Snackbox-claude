using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Snackbox.Api.Controllers;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;
using Snackbox.Api.Services;
using Xunit;

namespace Snackbox.Api.Tests.Services;

public class UserBalanceTests : IDisposable
{
    private readonly ApplicationDbContext _context = new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options);

    public UserBalanceTests()
    {
        _context.Users.AddRange(
            new User { Id = 1, Username = "Tina" },
            new User { Id = 2, Username = "Admin", IsAdmin = true });
        _context.SaveChanges();
    }

    [Fact]
    public async Task OpenAmount_ScansManualAndDiscounts_MinusPayments()
    {
        _context.Purchases.AddRange(
            new Purchase { Id = 1, UserId = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new Purchase { Id = 2, UserId = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, ManualAmount = 2m });
        _context.BarcodeScans.AddRange(
            new BarcodeScan { PurchaseId = 1, BarcodeId = 1, Amount = 1.20m },
            new BarcodeScan { PurchaseId = 1, BarcodeId = 1, Amount = 0.80m });
        _context.PurchaseDiscounts.Add(new PurchaseDiscount { PurchaseId = 1, DiscountId = 1, DiscountAmount = 0.60m });
        _context.Payments.Add(new Payment { UserId = 1, Amount = 1m, PaidAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        Assert.Equal(2.40m, await UserBalance.OpenAmountAsync(_context, 1));
        Assert.Equal(2.40m, (await UserBalance.OpenAmountsAsync(_context))[1]);
    }

    [Fact]
    public async Task PayPalPayment_LowersThePayersDebt_AndTheReceivingAdminNowOwesIt()
    {
        await CreatePaymentAsync(new CreatePaymentDto { UserId = 1, Amount = 10m, Type = "PayPal", AdminUserId = 2 });

        Assert.Equal(-10m, await UserBalance.OpenAmountAsync(_context, 1));
        Assert.Equal(10m, await UserBalance.OpenAmountAsync(_context, 2));
    }

    [Fact]
    public async Task Payment_ReceivedOnAnEarlierDay_IsBookedOnThatDay()
    {
        var day = DateOnly.FromDateTime(DateTime.Today).AddDays(-3);
        await CreatePaymentAsync(new CreatePaymentDto { UserId = 1, Amount = 5m, Type = "PayPal", AdminUserId = 2, PaidOn = day });

        var payment = await _context.Payments.SingleAsync();
        Assert.Equal(day.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Local).ToUniversalTime(), payment.PaidAt);
        Assert.Equal(payment.PaidAt, (await _context.Withdrawals.SingleAsync()).WithdrawnAt);
        Assert.Equal(payment.PaidAt, (await _context.Deposits.SingleAsync()).DepositedAt);
    }

    [Fact]
    public async Task Payment_ReceivedToday_KeepsTheTimeItWasEntered()
    {
        var before = DateTime.UtcNow;
        await CreatePaymentAsync(new CreatePaymentDto { UserId = 1, Amount = 5m, PaidOn = DateOnly.FromDateTime(DateTime.Today) });

        Assert.InRange((await _context.Payments.SingleAsync()).PaidAt, before, DateTime.UtcNow);
    }

    [Fact]
    public async Task Payment_InTheFuture_IsRefused()
    {
        var result = await Controller().Create(new CreatePaymentDto { UserId = 1, Amount = 5m, PaidOn = DateOnly.FromDateTime(DateTime.Today).AddDays(1) });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.False(await _context.Payments.AnyAsync());
    }

    private async Task CreatePaymentAsync(CreatePaymentDto dto) =>
        Assert.IsType<CreatedAtActionResult>((await Controller().Create(dto)).Result);

    private PaymentsController Controller() => new(_context, NullLogger<PaymentsController>.Instance);

    public void Dispose() => _context.Dispose();
}
