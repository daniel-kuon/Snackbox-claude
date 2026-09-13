using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Snackbox.Api.Controllers;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;
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

        _context.FeatureFlags.Add(new FeatureFlag
        {
            Id = 1,
            Key = FeatureFlagKeys.MobileApp,
            Name = "Snackbox on the phone",
            Description = "Phone install guide",
            IsEnabled = false,
            UpdatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();

        _controller = new FeatureFlagsController(_context, NullLogger<FeatureFlagsController>.Instance);
    }

    [Fact]
    public async Task GetAll_ReturnsFlagsWithState()
    {
        var result = await _controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var flags = Assert.IsType<List<FeatureFlagDto>>(ok.Value);
        var flag = Assert.Single(flags);
        Assert.Equal(FeatureFlagKeys.MobileApp, flag.Key);
        Assert.False(flag.IsEnabled);
    }

    [Fact]
    public async Task Update_TogglesFlag()
    {
        var result = await _controller.Update(FeatureFlagKeys.MobileApp, new UpdateFeatureFlagDto { IsEnabled = true });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<FeatureFlagDto>(ok.Value);
        Assert.True(dto.IsEnabled);

        var stored = await _context.FeatureFlags.SingleAsync(f => f.Key == FeatureFlagKeys.MobileApp);
        Assert.True(stored.IsEnabled);
    }

    [Fact]
    public async Task Update_UnknownKey_ReturnsNotFound()
    {
        var result = await _controller.Update("nope", new UpdateFeatureFlagDto { IsEnabled = true });

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
