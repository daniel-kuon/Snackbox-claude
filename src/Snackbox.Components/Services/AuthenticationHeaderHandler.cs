using System.Net.Http.Headers;

namespace Snackbox.Components.Services;

public class AuthenticationHeaderHandler : DelegatingHandler
{
    private readonly IStorageService _storageService;
    private const string TokenKey = "auth_token";

    public AuthenticationHeaderHandler(IStorageService storageService)
    {
        _storageService = storageService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _storageService.GetAsync(TokenKey);

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
