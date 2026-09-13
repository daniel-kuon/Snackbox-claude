using Microsoft.EntityFrameworkCore;
using Snackbox.Api.Data;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;
using Snackbox.ServiceDefaults.Tracing;

namespace Snackbox.Api.Services;

public interface IFeatureFlagService
{
    /// <summary>Keys of the features this user may see (audience Everyone, plus BetaTesters when the user is one).</summary>
    Task<List<string>> GetEnabledForUserAsync(int userId);

    Task<bool> IsEnabledForUserAsync(string key, int userId);

    Task<List<FeatureFlag>> GetAllAsync();

    /// <summary>Changes who a feature is on for. Returns null when the key is unknown.</summary>
    Task<FeatureFlag?> SetAudienceAsync(string key, FeatureAudience audience);
}

/// <summary>
/// Single place that decides whether a feature is on for a given user, so the rollout rule
/// (off / beta testers / everyone) cannot drift between callers.
/// </summary>
[Traced(LogReturnValue = true)]
public class FeatureFlagService : IFeatureFlagService
{
    private readonly ApplicationDbContext _context;

    public FeatureFlagService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<string>> GetEnabledForUserAsync(int userId)
    {
        var isBetaTester = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.IsBetaTester)
            .FirstOrDefaultAsync();

        return await _context.FeatureFlags
            .Where(f => f.Audience == FeatureAudience.Everyone
                        || (isBetaTester && f.Audience == FeatureAudience.BetaTesters))
            .Select(f => f.Key)
            .ToListAsync();
    }

    public async Task<bool> IsEnabledForUserAsync(string key, int userId)
    {
        var enabled = await GetEnabledForUserAsync(userId);
        return enabled.Contains(key);
    }

    public Task<List<FeatureFlag>> GetAllAsync() =>
        _context.FeatureFlags.OrderBy(f => f.Name).ToListAsync();

    public async Task<FeatureFlag?> SetAudienceAsync(string key, FeatureAudience audience)
    {
        var flag = await _context.FeatureFlags.FirstOrDefaultAsync(f => f.Key == key);
        if (flag == null)
            return null;

        flag.Audience = audience;
        flag.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return flag;
    }
}
