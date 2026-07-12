using PagueVeloz.Application.DTOs.Transactions.Responses;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Enums;

namespace PagueVeloz.Application.DTOs.Accounts.Responses;

public record AccountResponse(
    Guid Id,
    Guid CustomerId,
    long AvailableBalance,
    long ReservedBalance,
    long CreditLimit,
    AccountStatus Status,
    IEnumerable<OperationResponse> Operations
)
{
    public static AccountResponse From(Account account) => new(
        account.Id,
        account.CustomerId,
        account.AvailableBalance,
        account.ReservedBalance,
        account.CreditLimit,
        account.Status,
        account.Operations.Select(OperationResponse.From)
    );
}
