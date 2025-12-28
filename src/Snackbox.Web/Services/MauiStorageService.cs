using Snackbox.Components.Services;

namespace Snackbox.Web.Services;

public class MauiStorageService : IStorageService
{
    private readonly ISecureStorage _secureStorage;

    public MauiStorageService(ISecureStorage secureStorage)
    {
        _secureStorage = secureStorage;
    }

    public async Task SetAsync(string key, string value)
    {
        await _secureStorage.SetAsync(key, value);
    }

    public async Task<string?> GetAsync(string key)
    {
        return await _secureStorage.GetAsync(key);
    }

    public void Remove(string key)
    {
        _secureStorage.Remove(key);
    }
}
