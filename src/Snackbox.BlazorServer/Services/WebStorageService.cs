using Snackbox.Components.Services;

namespace Snackbox.BlazorServer.Services;

public class WebStorageService : IStorageService
{
    private readonly Dictionary<string, string> _storage = new();

    public Task SetAsync(string key, string value)
    {
        _storage[key] = value;
        return Task.CompletedTask;
    }

    public Task<string?> GetAsync(string key)
    {
        _storage.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }

    public void Remove(string key)
    {
        _storage.Remove(key);
    }
}
