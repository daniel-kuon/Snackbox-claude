using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Snackbox.Api.Services;

namespace Snackbox.Api.Attributes;

/// <summary>
/// Authorization attribute that allows anonymous access only when the database doesn't exist.
/// When the database exists, requires the user to be an authenticated admin.
/// This implements IAllowAnonymous to bypass controller-level [Authorize] attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class AllowAnonymousIfNoDatabaseAttribute : Attribute, IAsyncAuthorizationFilter, IAllowAnonymous
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var backupService = context.HttpContext.RequestServices.GetService<IBackupService>();

        if (backupService == null)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            return;
        }

        // Check if database exists
        bool databaseExists;
        try
        {
            databaseExists = await backupService.CheckDatabaseExistsAsync();
        }
        catch
        {
            // If we can't check, assume database doesn't exist and allow access
            databaseExists = false;
        }

        // If database doesn't exist, allow access for anyone (for initial setup)
        if (!databaseExists)
        {
            return;
        }

        // Database exists - require authenticated admin user
        var user = context.HttpContext.User;

        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Check if user is admin (check Role claim for "Admin")
        var isAdmin = user.Claims.Any(c =>
            c.Type == ClaimTypes.Role && c.Value.Equals("Admin", StringComparison.OrdinalIgnoreCase));

        if (!isAdmin)
        {
            context.Result = new ForbidResult();
            return;
        }
    }
}
