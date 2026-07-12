using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Enums;

namespace PagueVeloz.Application.DTOs.Transactions.Responses;

public record TransactionResponse(
    Guid TransactionId,
    OperationStatus Status,
    long Balance,
    long ReservedBalance,
    long AvailableBalance,
    DateTime Timestamp,
    string? ErrorMessage)
{
    public static TransactionResponse From(Account account, AccountOperation operation) => new(
        operation.Id,
        operation.Status,
        account.AvailableBalance + account.ReservedBalance,
        account.ReservedBalance,
        account.AvailableBalance,
        operation.OccurredAt,
        operation.FailureReason);
}
