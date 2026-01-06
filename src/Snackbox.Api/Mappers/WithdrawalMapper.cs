using Riok.Mapperly.Abstractions;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;

namespace Snackbox.Api.Mappers;

[Mapper]
public static partial class WithdrawalMapper
{
    /// <summary>
    /// Maps a Withdrawal entity to a WithdrawalDto.
    /// Username is not mapped by Mapperly; must be set manually when User is loaded.
    /// </summary>
    [MapperIgnoreSource(nameof(Withdrawal.User))]
    [MapperIgnoreTarget(nameof(WithdrawalDto.Username))]
    public static partial WithdrawalDto ToDto(this Withdrawal source);

    /// <summary>
    /// Maps a Withdrawal entity to a WithdrawalDto including the Username from User navigation property.
    /// </summary>
    public static WithdrawalDto ToDtoWithUser(this Withdrawal source)
    {
        var dto = source.ToDto();
        dto.Username = source.User.Username;
        return dto;
    }

    /// <summary>
    /// Maps a list of Withdrawal entities to a list of WithdrawalDto including Username.
    /// </summary>
    public static List<WithdrawalDto> ToDtoListWithUser(this IEnumerable<Withdrawal> source)
    {
        return source.Select(w => w.ToDtoWithUser()).ToList();
    }
}
