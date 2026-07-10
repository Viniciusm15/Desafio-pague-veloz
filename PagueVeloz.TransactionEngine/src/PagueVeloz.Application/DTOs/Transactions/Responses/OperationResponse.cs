using PagueVeloz.Domain.Entities;

namespace PagueVeloz.Application.DTOs.Transactions.Responses;

public record OperationResponse(
    Guid Id,
    Guid AccountId,
    string Type,
    decimal Amount,
    string Currency,
    string? Metadata,
    string Status,
    string ReferenceId,
    string? FailureReason,
    DateTime OccurredAt
)
{
    public static OperationResponse From(AccountOperation operation) => new(
        operation.Id,
        operation.AccountId,
        operation.Type.ToString(),
        operation.Amount,
        operation.Currency,
        operation.Metadata,
        operation.Status.ToString(),
        operation.ReferenceId,
        operation.FailureReason,
        operation.OccurredAt
    );
}
