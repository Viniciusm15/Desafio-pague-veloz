using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Enums;

namespace PagueVeloz.Application.DTOs.Transactions.Responses;

public record OperationResponse(
    Guid Id,
    Guid AccountId,
    OperationType Type,
    long Amount,
    string Currency,
    string? Metadata,
    OperationStatus Status,
    string ReferenceId,
    string? FailureReason,
    DateTime OccurredAt
)
{
    public static OperationResponse From(AccountOperation operation) => new(
        operation.Id,
        operation.AccountId,
        operation.Type,
        operation.Amount,
        operation.Currency,
        operation.Metadata,
        operation.Status,
        operation.ReferenceId,
        operation.FailureReason,
        operation.OccurredAt
    );
}
