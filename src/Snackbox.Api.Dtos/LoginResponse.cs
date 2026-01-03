namespace Snackbox.Api.Dtos;

public class LoginResponse
{
    public required string Token { get; set; }
    public required string Username { get; set; }
    public string? Email { get; set; }
    public bool IsAdmin { get; set; }
    public int UserId { get; set; }
}
