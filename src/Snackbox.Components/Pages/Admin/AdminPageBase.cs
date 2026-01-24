using Microsoft.AspNetCore.Components;
using Snackbox.Components.Services;

namespace Snackbox.Components.Pages.Admin;

public class AdminPageBase : ComponentBase
{
    [Inject]
    protected IAuthenticationService AuthService { get; set; } = null!;

    [Inject]
    protected NavigationManager NavManager { get; set; } = null!;

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        await base.SetParametersAsync(parameters);
        await EnsureAdminAccessAsync();
    }

    private async Task EnsureAdminAccessAsync()
    {
        var isAuthenticated = await AuthService.IsAuthenticatedAsync();
        if (!isAuthenticated)
        {
            NavManager.NavigateTo("/login", replace: true);
            return;
        }

        var userInfo = await AuthService.GetCurrentUserInfoAsync();
        if (userInfo == null)
        {
            NavManager.NavigateTo("/login", replace: true);
            return;
        }

        if (!userInfo.IsAdmin)
        {
            NavManager.NavigateTo("/home", replace: true);
    }
}
}
