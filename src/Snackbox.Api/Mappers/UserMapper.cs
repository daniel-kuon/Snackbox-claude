using Riok.Mapperly.Abstractions;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;

namespace Snackbox.Api.Mappers;

[Mapper]
public static partial class UserMapper
{
    /// <summary>
    /// Maps a User entity to a UserDto.
    /// Balance must be calculated separately and is not mapped automatically.
    /// </summary>
    [MapperIgnoreSource(nameof(User.PasswordHash))]
    [MapperIgnoreSource(nameof(User.Barcodes))]
    [MapperIgnoreSource(nameof(User.Purchases))]
    [MapperIgnoreSource(nameof(User.Payments))]
    [MapperIgnoreSource(nameof(User.Withdrawals))]
    [MapperIgnoreTarget(nameof(UserDto.Balance))]
    [MapperIgnoreTarget(nameof(UserDto.HasPurchases))]
    [MapperIgnoreSource(nameof(User.UserAchievements))]
    public static partial UserDto ToDto(this User source);

    /// <summary>
    /// Maps a User entity to a UserDto with calculated balance.
    /// </summary>
    /// <param name="source">The User entity.</param>
    /// <param name="balance">The calculated balance (payments - purchases).</param>
    public static UserDto ToDtoWithBalance(this User source, decimal balance)
    {
        var dto = source.ToDto();
        dto.Balance = balance;
        return dto;
    }

    /// <summary>
    /// Maps a CreateUserDto to a User entity.
    /// Password hashing must be done separately.
    /// </summary>
    public static User ToEntity(this CreateUserDto source)
    {
        return new User
        {
            Username = source.Username,
            Email = string.IsNullOrWhiteSpace(source.Email) ? null : source.Email,
            IsAdmin = source.IsAdmin,
            IsActive = source.IsActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates an existing User entity from an UpdateUserDto.
    /// </summary>
    public static void UpdateFromDto(this User target, UpdateUserDto source)
    {
        target.Username = source.Username;
        target.Email = source.Email;
        target.IsAdmin = source.IsAdmin;
        target.IsActive = source.IsActive;
    }
}
