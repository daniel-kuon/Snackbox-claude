using Snackbox.Api.DTOs;

namespace Snackbox.Api.Services;

public interface IAuthenticationService
{
    Task<LoginResponse?> AuthenticateAsync(string barcodeValue);
    Task<LoginResponse?> AuthenticateWithPasswordAsync(string username, string password);
    Task<LoginResponse?> AuthenticateWithBarcodeAndPasswordAsync(string barcodeValue, string password);
    Task<bool> SetPasswordAsync(string barcodeValue, string email, string newPassword);
    Task<bool> UserHasPasswordAsync(string username);
}
