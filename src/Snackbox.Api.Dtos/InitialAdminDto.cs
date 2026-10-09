using System.ComponentModel.DataAnnotations;

namespace Snackbox.Api.Dtos;

/// <summary>
/// The first administrator, entered while creating an empty database. Without it nobody could
/// log in to the fresh installation - and on a remote machine nobody can scan a card either.
/// </summary>
public class InitialAdminDto
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Username { get; set; } = "";

    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = "";

    [Required, StringLength(200, MinimumLength = 6)]  // same rule as register / change password
    public string Password { get; set; } = "";
}
