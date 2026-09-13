using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Snackbox.Api.Controllers;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;
using Snackbox.Api.Services;
using Xunit;

namespace Snackbox.Api.Tests.Controllers;

public class FeatureFlagsControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly FeatureFlagsController _controller;

    public FeatureFlagsControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _context.Users.Add(new User { Id = 1, Username = "regular", CreatedAt = DateTime.UtcNow });
        _context.Users.Add(new User { Id = 2, Username = "beta", IsBetaTester = true, CreatedAt = DateTime.UtcNow });
        _context.FeatureFlags.Add(new FeatureFlag
        {
            Id = 1,
            Key = FeatureFlagKeys.MobileApp,
            Name = "Snackbox on the phone",
            Description = "Phone install guide",
            Audience = FeatureAudience.Disabled,
            UpdatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();

        _controller = new FeatureFlagsController(
            new FeatureFlagService(_context),
            NullLogger<FeatureFlagsController>.Instance);
    }

    [Fact]
    public async Task GetAll_ReturnsFlagsWithTheirAudience()
    {
        var result = await _controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var flags = Assert.IsType<List<FeatureFlagDto>>(ok.Value);
        var flag = Assert.Single(flags);
        Assert.Equal(FeatureFlagKeys.MobileApp, flag.Key);
        Assert.Equal(FeatureAudience.Disabled, flag.Audience);
    }

    [Fact]
    public async Task Update_SetsTheAudience()
    {
        var result = await _controller.Update(FeatureFlagKeys.MobileApp,
            new UpdateFeatureFlagDto { Audience = FeatureAudience.Everyone });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<FeatureFlagDto>(ok.Value);
        Assert.Equal(FeatureAudience.Everyone, dto.Audience);

        var stored = await _context.FeatureFlags.SingleAsync(f => f.Key == FeatureFlagKeys.MobileApp);
        Assert.Equal(FeatureAudience.Everyone, stored.Audience);
    }

    [Fact]
    public async Task Update_UnknownKey_ReturnsNotFound()
    {
        var result = await _controller.Update("nope", new UpdateFeatureFlagDto { Audience = FeatureAudience.Everyone });

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetForUser_ReleasesBetaFeatureToBetaTestersOnly()
    {
        await _controller.Update(FeatureFlagKeys.MobileApp,
            new UpdateFeatureFlagDto { Audience = FeatureAudience.BetaTesters });

        var regular = Assert.IsType<OkObjectResult>((await _controller.GetForUser(1)).Result);
        var beta = Assert.IsType<OkObjectResult>((await _controller.GetForUser(2)).Result);

        Assert.Empty(Assert.IsType<List<string>>(regular.Value));
        Assert.Contains(FeatureFlagKeys.MobileApp, Assert.IsType<List<string>>(beta.Value));
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
