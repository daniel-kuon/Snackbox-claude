namespace Snackbox.Api.Dtos;

public class UserDto
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public string? Email { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; }
    public bool IsRetired { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RegisterUserDto
{
    public required string BarcodeValue { get; set; }
    public required string Name { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
}

public class CreateUserDto
{
    public required string Username { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateUserDto
{
    public required string Username { get; set; }
    public string? Email { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; }
}
