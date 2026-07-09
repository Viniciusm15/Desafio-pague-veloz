using PagueVeloz.Domain.Enums;
using System.Text.Json;

namespace PagueVeloz.Domain.Entities;

public class AccountOperation
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public OperationType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public string? Metadata { get; private set; }
    public OperationStatus Status { get; private set; }
    public string ReferenceId { get; private set; } = string.Empty;
    public string? FailureReason { get; private set; }
    public DateTime OccurredAt { get; private set; }

    private AccountOperation() { }

    public AccountOperation(
    Guid accountId,
    OperationType type,
    decimal amount,
    string currency,
    OperationStatus status,
    string referenceId,
    string? failureReason,
    Dictionary<string, object>? metadata = null)
    {
        Id = Guid.NewGuid();
        AccountId = accountId;
        Type = type;
        Amount = amount;
        Currency = currency;
        Status = status;
        ReferenceId = referenceId;
        FailureReason = failureReason;
        OccurredAt = DateTime.UtcNow;
        Metadata = metadata is not null ? JsonSerializer.Serialize(metadata) : null;
    }

    public static AccountOperation Succeeded(
        Guid accountId,
        OperationType type,
        decimal amount,
        string currency,
        string referenceId,
        Dictionary<string, object>? metadata = null)
        => new(accountId, type, amount, currency, OperationStatus.Success, referenceId, null, metadata);

    public static AccountOperation Failed(
        Guid accountId,
        OperationType type,
        decimal amount,
        string currency,
        string referenceId,
        string failureReason,
        Dictionary<string, object>? metadata = null)
        => new(accountId, type, amount, currency, OperationStatus.Failed, referenceId, failureReason, metadata);
}
