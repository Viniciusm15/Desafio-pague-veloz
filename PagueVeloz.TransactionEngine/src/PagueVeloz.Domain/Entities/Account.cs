using PagueVeloz.Domain.Enums;

namespace PagueVeloz.Domain.Entities;

public class Account
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal AvailableBalance { get; private set; }
    public decimal ReservedBalance { get; private set; }
    public decimal CreditLimit { get; private set; }
    public AccountStatus Status { get; private set; }

    private readonly List<AccountOperation> _operations = new();
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

        if (Status != AccountStatus.Active)
            return Fail(OperationType.Credit, amount, currency, referenceId, InactiveAccountReason(), metadata);

        if (amount <= 0)
            return Fail(OperationType.Credit, amount, currency, referenceId, "Amount must be greater than zero.", metadata);

        AvailableBalance += amount;

        var operation = AccountOperation.Succeeded(Id, OperationType.Credit, amount, currency, referenceId, metadata);
        _operations.Add(operation);

        return operation;
    }

    public AccountOperation Debit(decimal amount, string referenceId, string currency, Dictionary<string, object>? metadata = null)
    {
        if (TryGetExistingOperation(referenceId, out var existing))
            return existing!;

        if (Status != AccountStatus.Active)
            return Fail(OperationType.Debit, amount, currency, referenceId, InactiveAccountReason(), metadata);

        if (amount <= 0)
            return Fail(OperationType.Debit, amount, currency, referenceId, "Amount must be greater than zero.", metadata);

        var availableWithCreditLimit = AvailableBalance + CreditLimit;
        if (amount > availableWithCreditLimit)
            return Fail(OperationType.Debit, amount, currency, referenceId, "Insufficient funds to complete the debit.", metadata);

        AvailableBalance -= amount;

        var operation = AccountOperation.Succeeded(Id, OperationType.Debit, amount, currency, referenceId, metadata);
        _operations.Add(operation);

        return operation;
    }

    public AccountOperation Reserve(decimal amount, string referenceId, string currency, Dictionary<string, object>? metadata = null)
    {
        if (TryGetExistingOperation(referenceId, out var existing))
            return existing!;

        if (Status != AccountStatus.Active)
            return Fail(OperationType.Reserve, amount, currency, referenceId, InactiveAccountReason(), metadata);

        if (amount <= 0)
            return Fail(OperationType.Reserve, amount, currency, referenceId, "Amount must be greater than zero.", metadata);

        if (amount > AvailableBalance)
            return Fail(OperationType.Reserve, amount, currency, referenceId, "Insufficient available balance for reservation.", metadata);

        AvailableBalance -= amount;
        ReservedBalance += amount;

        var operation = AccountOperation.Succeeded(Id, OperationType.Reserve, amount, currency, referenceId, metadata);
        _operations.Add(operation);

        return operation;
    }

    public AccountOperation Capture(Guid reserveOperationId, string referenceId, string currency, Dictionary<string, object>? metadata = null)
    {
        if (TryGetExistingOperation(referenceId, out var existing))
            return existing!;

        if (Status != AccountStatus.Active)
            return Fail(OperationType.Capture, 0m, currency, referenceId, InactiveAccountReason(), metadata);

        var reservation = _operations.FirstOrDefault(o => o.Id == reserveOperationId);
        if (reservation is null)
            return Fail(OperationType.Capture, 0m, currency, referenceId, $"Reservation {reserveOperationId} not found.", metadata);

        var amount = reservation.Amount;

        if (amount > ReservedBalance)
            return Fail(OperationType.Capture, amount, currency, referenceId, "Insufficient reserved balance for capture.", metadata);

        ReservedBalance -= amount;

        var operation = AccountOperation.Succeeded(Id, OperationType.Capture, amount, currency, referenceId, metadata);
        _operations.Add(operation);

        return operation;
    }

    public AccountOperation Reversal(Guid originalOperationId, string referenceId, string currency, Dictionary<string, object>? metadata = null)
    {
        if (TryGetExistingOperation(referenceId, out var existing))
            return existing!;

        if (Status != AccountStatus.Active)
            return Fail(OperationType.Reversal, 0m, currency, referenceId, InactiveAccountReason(), metadata);

        var originalOperation = _operations.FirstOrDefault(o => o.Id == originalOperationId);
        if (originalOperation is null)
            return Fail(OperationType.Reversal, 0m, currency, referenceId, $"Operation {originalOperationId} not found.", metadata);

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
                    $"Operations of type '{originalOperation.Type}' cannot be reversed.", metadata);
        }

        var operation = AccountOperation.Succeeded(Id, OperationType.Reversal, amount, currency, referenceId, metadata);
        _operations.Add(operation);

        return operation;
    }

    #region Private Methods

    private AccountOperation Fail(
        OperationType type, decimal amount, string currency, string referenceId,
        string reason, Dictionary<string, object>? metadata)
    {
        var operation = AccountOperation.Failed(Id, type, amount, currency, referenceId, reason, metadata);
        _operations.Add(operation);
        return operation;
    }

    private string InactiveAccountReason()
        => $"Account {Id} is {Status}. Only active accounts can perform operations.";

    private bool TryGetExistingOperation(string referenceId, out AccountOperation? operation)
    {
        operation = _operations.FirstOrDefault(o => o.ReferenceId == referenceId);
        return operation is not null;
    }

    #endregion
}