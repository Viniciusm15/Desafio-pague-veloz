using PagueVeloz.Application.DTOs.Transactions.Responses;
using PagueVeloz.Domain.Entities;

namespace PagueVeloz.Application.DTOs.Accounts.Responses;

public record AccountResponse(
    Guid Id,
    Guid CustomerId,
    decimal AvailableBalance,
    decimal ReservedBalance,
    decimal CreditLimit,
    string Status,
    IEnumerable<OperationResponse> Operations
)
{
    public static AccountResponse From(Account account) => new(
        account.Id,
        account.CustomerId,
        account.AvailableBalance,
        account.ReservedBalance,
        account.CreditLimit,
        account.Status.ToString(),
        account.Operations.Select(OperationResponse.From)
    );
}
