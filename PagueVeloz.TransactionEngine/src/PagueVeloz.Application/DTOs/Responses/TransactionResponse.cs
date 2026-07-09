using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Enums;

namespace PagueVeloz.Application.DTOs.Responses;

public record TransactionResponse(
    Guid TransactionId,
    string Status,
    decimal Balance,
    decimal ReservedBalance,
    decimal AvailableBalance,
    DateTime Timestamp,
    string? ErrorMessage)
{
    public static TransactionResponse From(Account account, AccountOperation operation) => new(
        operation.Id,
        operation.Status == OperationStatus.Success ? "success" : "failed",
        account.AvailableBalance + account.ReservedBalance,
        account.ReservedBalance,
        account.AvailableBalance,
        operation.OccurredAt,
        operation.FailureReason);
}
