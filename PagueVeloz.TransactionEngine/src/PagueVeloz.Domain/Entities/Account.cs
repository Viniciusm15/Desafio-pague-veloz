using PagueVeloz.Domain.Common;
using PagueVeloz.Domain.Enums;
using PagueVeloz.Domain.Events;

namespace PagueVeloz.Domain.Entities;

public class Account : Entity
{
    public Guid CustomerId { get; private set; }
    public decimal AvailableBalance { get; private set; }
    public decimal ReservedBalance { get; private set; }
    public decimal CreditLimit { get; private set; }
    public AccountStatus Status { get; private set; }

    private readonly List<AccountOperation> _operations = [];
    public IReadOnlyCollection<AccountOperation> Operations => _operations.AsReadOnly();

    private Account(Guid customerId, decimal creditLimit)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        AvailableBalance = 0m;
        ReservedBalance = 0m;
        CreditLimit = creditLimit;
        Status = AccountStatus.Active;
    }

    public static Account Open(Guid customerId, decimal creditLimit = 0m)
    {
        if (creditLimit < 0)
            throw new ArgumentException("Credit limit cannot be negative.");
        return new Account(customerId, creditLimit);
    }

    public void Activate() => Status = AccountStatus.Active;
    public void Deactivate() => Status = AccountStatus.Inactive;
    public void Block() => Status = AccountStatus.Blocked;

    public AccountOperation Credit(decimal amount, string referenceId, string currency, Dictionary<string, object>? metadata = null)
    {
        if (TryGetExistingOperation(referenceId, out var existing))
            return existing!;

        var failure = Validate(
            OperationType.Credit,
            amount,
            currency,
            referenceId,
            metadata,
            () => Status != AccountStatus.Active ? InactiveAccountReason() : null,
            () => amount <= 0 ? "amount must be greater than zero." : null
        );

        if (failure is not null)
            return failure;

        AvailableBalance += amount;

        var operation = AccountOperation.Succeeded(Id, OperationType.Credit, amount, currency, referenceId, metadata);
        _operations.Add(operation);

        RaiseDomainEvent(new AccountCreditedEvent(
            AccountId: Id,
            OperationId: operation.Id,
            Amount: amount,
            Currency: currency,
            ReferenceId: referenceId
        ));

        return operation;
    }

    public AccountOperation Debit(decimal amount, string referenceId, string currency, Dictionary<string, object>? metadata = null)
    {
        if (TryGetExistingOperation(referenceId, out var existing))
            return existing!;

        var failure = Validate(
            OperationType.Debit,
            amount,
            currency,
            referenceId,
            metadata,
            () => Status != AccountStatus.Active ? InactiveAccountReason() : null,
            () => amount <= 0 ? "amount must be greater than zero." : null,
            () => amount > AvailableBalance + CreditLimit ? "insufficient funds to complete the debit." : null
        );

        if (failure is not null)
            return failure;

        AvailableBalance -= amount;

        var operation = AccountOperation.Succeeded(Id, OperationType.Debit, amount, currency, referenceId, metadata);
        _operations.Add(operation);

        RaiseDomainEvent(new AccountDebitedEvent(
            AccountId: Id,
            OperationId: operation.Id,
            Amount: amount,
            Currency: currency,
            ReferenceId: referenceId
        ));

        return operation;
    }

    public AccountOperation Reserve(decimal amount, string referenceId, string currency, Dictionary<string, object>? metadata = null)
    {
        if (TryGetExistingOperation(referenceId, out var existing))
            return existing!;

        var failure = Validate(
            OperationType.Reserve,
            amount,
            currency,
            referenceId,
            metadata,
            () => Status != AccountStatus.Active ? InactiveAccountReason() : null,
            () => amount <= 0 ? "amount must be greater than zero." : null,
            () => amount > AvailableBalance ? "insufficient available balance for reservation." : null
        );

        if (failure is not null)
            return failure;

        AvailableBalance -= amount;
        ReservedBalance += amount;

        var operation = AccountOperation.Succeeded(Id, OperationType.Reserve, amount, currency, referenceId, metadata);
        _operations.Add(operation);

        RaiseDomainEvent(new FundsReservedEvent(
            AccountId: Id,
            OperationId: operation.Id,
            Amount: amount,
            Currency: currency,
            ReferenceId: referenceId
        ));

        return operation;
    }

    public AccountOperation Capture(Guid reserveOperationId, string referenceId, string currency, Dictionary<string, object>? metadata = null)
    {
        if (TryGetExistingOperation(referenceId, out var existing))
            return existing!;

        var failure = Validate(
            OperationType.Capture,
            0m,
            currency,
            referenceId,
            metadata,
            () => Status != AccountStatus.Active ? InactiveAccountReason() : null,
            () =>
            {
                var reservation = _operations.FirstOrDefault(o => o.Id == reserveOperationId);
                return reservation is null ? $"reservation {reserveOperationId} not found." : null;
            },
            () =>
            {
                var reservation = _operations.FirstOrDefault(o => o.Id == reserveOperationId);
                if (reservation is null) return null;
                return reservation.Amount > ReservedBalance ? "insufficient reserved balance for capture." : null;
            }
        );

        if (failure is not null)
            return failure;

        var amount = _operations.First(o => o.Id == reserveOperationId).Amount;
        ReservedBalance -= amount;

        var operation = AccountOperation.Succeeded(Id, OperationType.Capture, amount, currency, referenceId, metadata);
        _operations.Add(operation);

        RaiseDomainEvent(new FundsCapturedEvent(
            AccountId: Id,
            OperationId: operation.Id,
            ReservationOperationId: reserveOperationId,
            Amount: amount,
            Currency: currency,
            ReferenceId: referenceId
        ));

        return operation;
    }

    public AccountOperation Reversal(Guid originalOperationId, string referenceId, string currency, Dictionary<string, object>? metadata = null)
    {
        if (TryGetExistingOperation(referenceId, out var existing))
            return existing!;

        var failure = Validate(
            OperationType.Reversal,
            0m,
            currency,
            referenceId,
            metadata,
            () => Status != AccountStatus.Active ? InactiveAccountReason() : null,
            () =>
            {
                var op = _operations.FirstOrDefault(o => o.Id == originalOperationId);
                return op is null ? $"operation {originalOperationId} not found." : null;
            }
        );

        if (failure is not null)
            return failure;

        var originalOperation = _operations.First(o => o.Id == originalOperationId);
        var amount = originalOperation.Amount;

        switch (originalOperation.Type)
        {
            case OperationType.Credit:
                AvailableBalance -= amount;
                break;
            case OperationType.Debit:
                AvailableBalance += amount;
                break;
            case OperationType.Reserve:
                ReservedBalance -= amount;
                AvailableBalance += amount;
                break;
            case OperationType.Capture:
                AvailableBalance += amount;
                break;
            default:
                return Fail(OperationType.Reversal, amount, currency, referenceId,
                    $"operations of type '{originalOperation.Type}' cannot be reversed.", metadata);
        }

        var operation = AccountOperation.Succeeded(Id, OperationType.Reversal, amount, currency, referenceId, metadata);
        _operations.Add(operation);

        RaiseDomainEvent(new OperationReversedEvent(
            AccountId: Id,
            OperationId: operation.Id,
            OriginalOperationId: originalOperationId,
            Amount: amount,
            Currency: currency,
            ReferenceId: referenceId
        ));

        return operation;
    }

    #region Private Methods

    private AccountOperation? Validate(
        OperationType type,
        decimal amount,
        string currency,
        string referenceId,
        Dictionary<string, object>? metadata,
        params Func<string?>[] validations)
    {
        if (string.IsNullOrWhiteSpace(referenceId))
            return Fail(type, amount, currency, referenceId, "reference_id must not be empty.", metadata);

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            return Fail(type, amount, currency, referenceId, "currency must be a valid 3-letter ISO 4217 code.", metadata);

        foreach (var validate in validations)
        {
            var error = validate();
            if (error is not null)
                return Fail(type, amount, currency, referenceId, error, metadata);
        }

        return null;
    }

    private AccountOperation Fail(
        OperationType type, decimal amount, string currency, string referenceId,
        string reason, Dictionary<string, object>? metadata)
    {
        var operation = AccountOperation.Failed(Id, type, amount, currency, referenceId, reason, metadata);
        _operations.Add(operation);
        return operation;
    }

    private string InactiveAccountReason()
        => $"account {Id} is {Status}. Only active accounts can perform operations.";

    private bool TryGetExistingOperation(string referenceId, out AccountOperation? operation)
    {
        operation = _operations.FirstOrDefault(o => o.ReferenceId == referenceId);
        return operation is not null;
    }

    #endregion
}
