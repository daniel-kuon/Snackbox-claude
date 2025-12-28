namespace Snackbox.Components.Services;

public interface IStorageService
{
    Task SetAsync(string key, string value);
    Task<string?> GetAsync(string key);
    void Remove(string key);
}
