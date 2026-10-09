using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Snackbox.Api.Controllers;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;
using Snackbox.Api.Services;
using Xunit;

namespace Snackbox.Api.Tests.Controllers;

public class UsersControllerSetupTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UsersController _controller;

    public UsersControllerSetupTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _controller = new UsersController(_context, NullLogger<UsersController>.Instance, Mock.Of<IAuthenticationService>());

        SeedTestData();
    }

    private void SeedTestData()
    {
        var placeholderUser = new User
        {
            Id = 1,
            Username = "User 1",
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        var activeUser = new User
        {
            Id = 2,
            Username = "Existing User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.AddRange(placeholderUser, activeUser);
        _context.Barcodes.Add(new PurchaseBarcode { Id = 1, UserId = 1, Code = "NEW-CARD-50", Amount = 0.50m, CreatedAt = DateTime.UtcNow });
        _context.Barcodes.Add(new PurchaseBarcode { Id = 2, UserId = 2, Code = "OLD-CARD-50", Amount = 0.50m, CreatedAt = DateTime.UtcNow });
        _context.SaveChanges();
    }

    [Fact]
    public async Task CompleteSetup_InactiveUser_ActivatesAndRenames()
    {
        var result = await _controller.CompleteSetup(new CompleteAccountSetupDto
        {
            BarcodeCode = "NEW-CARD-50",
            Username = "Jane Smith",
            Email = "jane@example.com"
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<CompleteAccountSetupResponse>(ok.Value);
        Assert.Equal("Jane Smith", response.Username);

        var user = await _context.Users.SingleAsync(u => u.Id == 1);
        Assert.True(user.IsActive);
        Assert.Equal("Jane Smith", user.Username);
        Assert.Equal("jane@example.com", user.Email);
    }

    [Fact]
    public async Task CompleteSetup_UnknownBarcode_ReturnsNotFound()
    {
        var result = await _controller.CompleteSetup(new CompleteAccountSetupDto
        {
            BarcodeCode = "DOES-NOT-EXIST",
            Username = "Jane Smith",
            Email = "someone@example.com"
        });

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CompleteSetup_AlreadyActiveUser_ReturnsBadRequest()
    {
        var result = await _controller.CompleteSetup(new CompleteAccountSetupDto
        {
            BarcodeCode = "OLD-CARD-50",
            Username = "New Name",
            Email = "someone@example.com"
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        var user = await _context.Users.SingleAsync(u => u.Id == 2);
        Assert.Equal("Existing User", user.Username);
    }

    [Fact]
    public async Task CompleteSetup_UsernameTakenByOtherUser_ReturnsBadRequest()
    {
        var result = await _controller.CompleteSetup(new CompleteAccountSetupDto
        {
            BarcodeCode = "NEW-CARD-50",
            Username = "Existing User",
            Email = "someone@example.com"
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        var user = await _context.Users.SingleAsync(u => u.Id == 1);
        Assert.False(user.IsActive);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    [InlineData("not-an-address")]
    public async Task CompleteSetup_WithoutValidEmail_ReturnsBadRequest(string? email)
    {
        var result = await _controller.CompleteSetup(new CompleteAccountSetupDto
        {
            BarcodeCode = "NEW-CARD-50",
            Username = "Jane Smith",
            Email = email
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.False((await _context.Users.SingleAsync(u => u.Id == 1)).IsActive);
    }

    [Fact]
    public async Task CompleteSetup_BlankUsername_ReturnsBadRequest()
    {
        var result = await _controller.CompleteSetup(new CompleteAccountSetupDto
        {
            BarcodeCode = "NEW-CARD-50",
            Username = "   "
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task MarkWizardStepsSeen_RecordsStepsForBarcodeOwner()
    {
        var result = await _controller.MarkWizardStepsSeen(new MarkWizardStepsSeenDto
        {
            BarcodeCode = "OLD-CARD-50",
            StepKeys = ["buying", "paying"]
        });

        Assert.IsType<OkResult>(result);
        var seen = await _context.UserWizardSteps.Where(s => s.UserId == 2).Select(s => s.StepKey).ToListAsync();
        Assert.Equal(new[] { "buying", "paying" }, seen.Order());
    }

    [Fact]
    public async Task MarkWizardStepsSeen_IgnoresUnknownKeysAndDuplicates()
    {
        await _controller.MarkWizardStepsSeen(new MarkWizardStepsSeenDto
        {
            BarcodeCode = "OLD-CARD-50",
            StepKeys = ["buying"]
        });

        // Re-sending an already recorded step must not violate the unique index
        var result = await _controller.MarkWizardStepsSeen(new MarkWizardStepsSeenDto
        {
            BarcodeCode = "OLD-CARD-50",
            StepKeys = ["buying", "not_a_real_step", "paying"]
        });

        Assert.IsType<OkResult>(result);
        var seen = await _context.UserWizardSteps.Where(s => s.UserId == 2).Select(s => s.StepKey).ToListAsync();
        Assert.Equal(new[] { "buying", "paying" }, seen.Order());
    }

    [Fact]
    public async Task MarkWizardStepsSeen_UnknownBarcode_ReturnsNotFound()
    {
        var result = await _controller.MarkWizardStepsSeen(new MarkWizardStepsSeenDto
        {
            BarcodeCode = "DOES-NOT-EXIST",
            StepKeys = ["buying"]
        });

        Assert.IsType<NotFoundObjectResult>(result);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
