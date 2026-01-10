using Refit;
using Snackbox.Api.Dtos;

namespace Snackbox.Components.Services;

/// <summary>
/// Helper service for handling API errors consistently across the application
/// </summary>
public static class ApiErrorHelper
{
    /// <summary>
    /// Extracts a user-friendly error message from an API exception
    /// </summary>
    public static async Task<string> GetErrorMessageAsync(ApiException ex)
    {
        try
        {
            var errorResponse = await ex.GetContentAsAsync<ErrorResponse>();
            return errorResponse?.Message ?? ex.Message;
        }
        catch
        {
            return ex.Message;
        }
    }

    /// <summary>
    /// Extracts a user-friendly error message from an exception
    /// </summary>
    public static string GetErrorMessage(Exception ex)
    {
        return ex.Message;
    }
}
