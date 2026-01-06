using Snackbox.Api.Dtos;
using Snackbox.Api.Models;

namespace Snackbox.Api.Mappers;

public static class DepositMapper
{
    public static DepositDto ToDto(this Deposit deposit)
    {
        return new DepositDto
        {
            Id = deposit.Id,
            UserId = deposit.UserId,
            Username = deposit.User?.Username ?? string.Empty,
            Amount = deposit.Amount,
            DepositedAt = deposit.DepositedAt,
            LinkedPaymentId = deposit.LinkedPaymentId
        };
    }

    public static List<DepositDto> ToDtoList(this IEnumerable<Deposit> deposits)
    {
        return deposits.Select(d => d.ToDto()).ToList();
    }
}
