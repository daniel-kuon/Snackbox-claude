using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;
using Snackbox.Api.Services;
using Xunit;

namespace Snackbox.Api.Tests.Services;

public class FeatureFlagServiceTests : IDisposable
{
    private const int RegularUserId = 1;
    private const int BetaUserId = 2;

    private readonly ApplicationDbContext _context;
    private readonly FeatureFlagService _service;

    public FeatureFlagServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _context.Users.Add(new User { Id = RegularUserId, Username = "regular", CreatedAt = DateTime.UtcNow });
        _context.Users.Add(new User { Id = BetaUserId, Username = "beta", IsBetaTester = true, CreatedAt = DateTime.UtcNow });
        _context.FeatureFlags.AddRange(
            Flag(10, "off_feature", FeatureAudience.Disabled),
            Flag(11, "beta_feature", FeatureAudience.BetaTesters),
            Flag(12, "public_feature", FeatureAudience.Everyone));
        _context.SaveChanges();

        _service = new FeatureFlagService(_context);
    }

    private static FeatureFlag Flag(int id, string key, FeatureAudience audience) => new()
    {
        Id = id,
        Key = key,
        Name = key,
        Description = key,
        Audience = audience,
        UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task RegularUser_SeesOnlyFeaturesReleasedToEveryone()
    {
        var enabled = await _service.GetEnabledForUserAsync(RegularUserId);

        Assert.Equal(new[] { "public_feature" }, enabled);
    }

    [Fact]
    public async Task BetaTester_AlsoSeesBetaFeatures_ButNeverDisabledOnes()
    {
        var enabled = await _service.GetEnabledForUserAsync(BetaUserId);

        Assert.Equal(new[] { "beta_feature", "public_feature" }, enabled.Order());
        Assert.DoesNotContain("off_feature", enabled);
    }

    [Fact]
    public async Task IsEnabledForUser_RespectsTheBetaRule()
    {
        Assert.False(await _service.IsEnabledForUserAsync("beta_feature", RegularUserId));
        Assert.True(await _service.IsEnabledForUserAsync("beta_feature", BetaUserId));
        Assert.True(await _service.IsEnabledForUserAsync("public_feature", RegularUserId));
        Assert.False(await _service.IsEnabledForUserAsync("off_feature", BetaUserId));
    }

    [Fact]
    public async Task UnknownUser_SeesOnlyPublicFeatures()
    {
        // e.g. a scan of a card whose user was just deleted - must not leak beta features
        var enabled = await _service.GetEnabledForUserAsync(9999);

        Assert.Equal(new[] { "public_feature" }, enabled);
    }

    [Fact]
    public async Task SetAudience_ChangesWhoSeesTheFeature()
    {
        var updated = await _service.SetAudienceAsync("off_feature", FeatureAudience.BetaTesters);

        Assert.NotNull(updated);
        Assert.Equal(FeatureAudience.BetaTesters, updated!.Audience);
        Assert.Contains("off_feature", await _service.GetEnabledForUserAsync(BetaUserId));
        Assert.DoesNotContain("off_feature", await _service.GetEnabledForUserAsync(RegularUserId));
    }

    [Fact]
    public async Task SetAudience_UnknownKey_ReturnsNull()
    {
        Assert.Null(await _service.SetAudienceAsync("nope", FeatureAudience.Everyone));
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
