using Refit;
using Snackbox.Api.Dtos;

namespace Snackbox.ApiClient;

/// <summary>
/// Users API endpoints
/// </summary>
public interface IUsersApi
{
    [Get("/api/users")]
    Task<IEnumerable<UserDto>> GetAllAsync();
    
    [Get("/api/users/{id}")]
    Task<UserDto> GetByIdAsync(int id);
    
    [Post("/api/users/register")]
    Task<RegisterResponse> RegisterAsync([Body] RegisterUserDto dto);
    
    [Post("/api/users")]
    Task<UserDto> CreateAsync([Body] CreateUserDto dto);
    
    [Put("/api/users/{id}")]
    Task<UserDto> UpdateAsync(int id, [Body] UpdateUserDto dto);
    
    [Delete("/api/users/{id}")]
    Task DeleteAsync(int id);
}

public class RegisterResponse
{
    public string? Message { get; set; }
    public int UserId { get; set; }
}
