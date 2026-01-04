using Riok.Mapperly.Abstractions;
using Snackbox.Api.Dtos;
using Snackbox.Api.Models;

namespace Snackbox.Api.Mappers;

[Mapper]
public static partial class PaymentMapper
{
    /// <summary>
    /// Maps a Payment entity to a PaymentDto.
    /// Username is not mapped by Mapperly; must be set manually when User is loaded.
    /// </summary>
    [MapperIgnoreSource(nameof(Payment.User))]
    [MapperIgnoreTarget(nameof(PaymentDto.Username))]
    public static partial PaymentDto ToDto(this Payment source);

    /// <summary>
    /// Maps a Payment entity to a PaymentDto including the Username from User navigation property.
    /// </summary>
    public static PaymentDto ToDtoWithUser(this Payment source)
    {
        var dto = source.ToDto();
        dto.Username = source.User.Username;
        return dto;
    }

    /// <summary>
    /// Maps a list of Payment entities to a list of PaymentDto including Username.
    /// </summary>
    public static List<PaymentDto> ToDtoListWithUser(this IEnumerable<Payment> source)
    {
        return source.Select(p => p.ToDtoWithUser()).ToList();
    }

    /// <summary>
    /// Maps a CreatePaymentDto to a Payment entity.
    /// PaidAt is set to current UTC time.
    /// </summary>
    public static Payment ToEntity(this CreatePaymentDto source)
    {
        return new Payment
        {
            UserId = source.UserId,
            Amount = source.Amount,
            Notes = source.Notes,
            PaidAt = DateTime.UtcNow
        };
    }
}
