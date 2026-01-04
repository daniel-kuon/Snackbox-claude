using Snackbox.Api.Dtos;
using Snackbox.Api.Mappers;
using Snackbox.Api.Models;
using Xunit;

namespace Snackbox.Api.Tests.Mappers;

public class UserMapperTests
{
    [Fact]
    public void ToDto_User_MapsAllPropertiesExceptPasswordAndBalance()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            IsAdmin = true,
            CreatedAt = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            PasswordHash = "hashedpassword123"
        };

        // Act
        var dto = user.ToDto();

        // Assert
        Assert.Equal(1, dto.Id);
        Assert.Equal("testuser", dto.Username);
        Assert.Equal("test@example.com", dto.Email);
        Assert.True(dto.IsAdmin);
        Assert.Equal(new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc), dto.CreatedAt);
        Assert.Equal(0, dto.Balance); // Not mapped
    }

    [Fact]
    public void ToDtoWithBalance_User_MapsBalanceCorrectly()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            IsAdmin = false,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var dto = user.ToDtoWithBalance(150.50m);

        // Assert
        Assert.Equal(150.50m, dto.Balance);
        Assert.Equal("testuser", dto.Username);
    }

    [Fact]
    public void ToDtoWithBalance_User_WithNegativeBalance()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var dto = user.ToDtoWithBalance(-25.00m);

        // Assert
        Assert.Equal(-25.00m, dto.Balance);
    }

    [Fact]
    public void ToEntity_CreateUserDto_CreatesUserWithCorrectValues()
    {
        // Arrange
        var dto = new CreateUserDto
        {
            Username = "newuser",
            Email = "new@example.com",
            IsAdmin = true
        };

        // Act
        var user = dto.ToEntity();

        // Assert
        Assert.Equal("newuser", user.Username);
        Assert.Equal("new@example.com", user.Email);
        Assert.True(user.IsAdmin);
        Assert.True((DateTime.UtcNow - user.CreatedAt).TotalSeconds < 5); // Created recently
    }

    [Fact]
    public void ToEntity_CreateUserDto_WithEmptyEmail_SetsNullEmail()
    {
        // Arrange
        var dto = new CreateUserDto
        {
            Username = "newuser",
            Email = "  ",
            IsAdmin = false
        };

        // Act
        var user = dto.ToEntity();

        // Assert
        Assert.Null(user.Email);
    }

    [Fact]
    public void UpdateFromDto_User_UpdatesAllMappableProperties()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "oldname",
            Email = "old@example.com",
            IsAdmin = false,
            CreatedAt = DateTime.UtcNow
        };

        var dto = new UpdateUserDto
        {
            Username = "newname",
            Email = "new@example.com",
            IsAdmin = true
        };

        // Act
        user.UpdateFromDto(dto);

        // Assert
        Assert.Equal("newname", user.Username);
        Assert.Equal("new@example.com", user.Email);
        Assert.True(user.IsAdmin);
        Assert.Equal(1, user.Id); // Id should not change
    }
}
