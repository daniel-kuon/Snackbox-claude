using Snackbox.Api.Dtos;

namespace Snackbox.Api.Services;

public interface IAuthenticationService
{
    Task<LoginResponse?> AuthenticateAsync(string barcodeValue);
    Task<LoginResponse?> AuthenticateWithPasswordAsync(string username, string password);
    Task<LoginResponse?> AuthenticateWithBarcodeAndPasswordAsync(string barcodeValue, string password);
    // Set or reset password using email + any active barcode of the user
    Task<bool> SetPasswordAsync(string barcodeValue, string email, string newPassword);
    // Change password for currently authenticated user (requires current password)
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    // Admin can set password for any user without knowing current password
    Task<bool> AdminSetPasswordAsync(int userId, string newPassword);
    Task<bool> UserHasPasswordAsync(string username);
}
