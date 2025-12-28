namespace Snackbox.Api.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public bool IsAdmin { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateUserDto
{
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public bool IsAdmin { get; set; }
}

public class UpdateUserDto
{
    public required string Username { get; set; }
    public required string Email { get; set; }
    public bool IsAdmin { get; set; }
}
