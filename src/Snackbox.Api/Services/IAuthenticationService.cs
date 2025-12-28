using Snackbox.Api.DTOs;

namespace Snackbox.Api.Services;

public interface IAuthenticationService
{
    Task<LoginResponse?> AuthenticateAsync(string barcodeValue);
}
