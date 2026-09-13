using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Snackbox.ServiceDefaults;

namespace Snackbox.Components.Services;

public interface IUiTelemetry
{
    Task<Activity?> StartUiActionAsync(string actionName, string? component = null, string? route = null,
        IDictionary<string, object?>? tags = null);
    Task<Activity?> StartHttpDependencyAsync(HttpRequestMessage request, IDictionary<string, object?>? tags = null);
    void SetUserTags(Activity? activity, UserInfo userInfo);
}

/// <summary>
/// Starts UI-level activities (spans) tagged with the current user. Reads the stored login
/// info directly from <see cref="IStorageService"/> rather than via <see cref="IAuthenticationService"/>,
/// because that service itself depends on telemetry - going through it would be a DI cycle.
/// </summary>
public sealed class UiTelemetry : IUiTelemetry
{
    private static readonly ActivitySource ActivitySource = new(TelemetrySources.Ui);
    private const string UserInfoKey = "user_info"; // same key AuthenticationService writes

    private readonly IStorageService _storageService;
    private readonly TelemetryOptions _options;

    public UiTelemetry(IStorageService storageService, IOptions<TelemetryOptions> options)
    {
        _storageService = storageService;
        _options = options.Value;
    }

    public async Task<Activity?> StartUiActionAsync(string actionName, string? component = null, string? route = null,
        IDictionary<string, object?>? tags = null)
    {
        var activity = ActivitySource.StartActivity($"ui.{actionName}", ActivityKind.Internal);
        if (activity == null)
        {
            return null;
        }

        activity.SetTag("ui.action", actionName);
        if (!string.IsNullOrWhiteSpace(component))
        {
            activity.SetTag("ui.component", component);
        }

        if (!string.IsNullOrWhiteSpace(route))
        {
            activity.SetTag("ui.route", route);
        }

        if (tags != null)
        {
            foreach (var tag in tags)
            {
                activity.SetTag(tag.Key, tag.Value);
            }
        }

        var userInfo = await TryGetCurrentUserAsync();
        if (userInfo != null)
        {
            SetUserTags(activity, userInfo);
        }

        return activity;
    }

    public async Task<Activity?> StartHttpDependencyAsync(HttpRequestMessage request, IDictionary<string, object?>? tags = null)
    {
        // Only create a root span for "bare" HTTP calls; calls made inside a UI action are
        // already covered by that parent span plus HttpClient instrumentation.
        if (Activity.Current != null)
        {
            return null;
        }

        var activity = await StartUiActionAsync("http.request", component: "http", tags: tags);
        if (activity != null)
        {
            activity.SetTag("http.method", request.Method.Method);
            if (request.RequestUri != null)
            {
                activity.SetTag("http.url", request.RequestUri.ToString());
            }
        }

        return activity;
    }

    public void SetUserTags(Activity? activity, UserInfo userInfo)
    {
        if (activity == null)
        {
            return;
        }

        if (_options.UserIdentityMode == UserIdentityMode.UserId)
        {
            if (userInfo.UserId != 0)
            {
                activity.SetTag("user.id", userInfo.UserId);
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(userInfo.Username))
        {
            activity.SetTag("user.username", userInfo.Username);
            activity.SetTag("user.full_name", userInfo.Username);
        }

        if (!string.IsNullOrWhiteSpace(userInfo.Email))
        {
            activity.SetTag("user.email", userInfo.Email);
        }
    }

    private async Task<UserInfo?> TryGetCurrentUserAsync()
    {
        try
        {
            var json = await _storageService.GetAsync(UserInfoKey);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            var stored = JsonSerializer.Deserialize<StoredLogin>(json);
            if (stored == null)
            {
                return null;
            }

            return new UserInfo
            {
                UserId = stored.UserId,
                Username = stored.Username ?? string.Empty,
                Email = stored.Email,
                IsAdmin = stored.IsAdmin
            };
        }
        catch
        {
            // Telemetry must never break the UI
            return null;
        }
    }

    // Mirrors the shape AuthenticationService serialises into storage
    private sealed class StoredLogin
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public bool IsAdmin { get; set; }
        public int UserId { get; set; }
    }
}
