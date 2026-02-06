using System.Diagnostics;
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

public sealed class UiTelemetry : IUiTelemetry
{
    private static readonly ActivitySource ActivitySource = new(TelemetrySources.Ui);
    private readonly IAuthenticationService _authenticationService;
    private readonly TelemetryOptions _options;

    public UiTelemetry(IAuthenticationService authenticationService, IOptions<TelemetryOptions> options)
    {
        _authenticationService = authenticationService;
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

        var userInfo = await _authenticationService.GetCurrentUserInfoAsync();
        if (userInfo != null)
        {
            SetUserTags(activity, userInfo);
        }

        return activity;
    }

    public async Task<Activity?> StartHttpDependencyAsync(HttpRequestMessage request, IDictionary<string, object?>? tags = null)
    {
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
}
