using PagueVeloz.Domain.Enums;

namespace PagueVeloz.Domain.Entities;

public class Account
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal AvailableBalance { get; private set; }
    public decimal ReservedBalance { get; private set; }
    public decimal CreditLimit { get; private set; }

    private readonly List<AccountOperation> _operations = new();
    public IReadOnlyCollection<AccountOperation> Operations => _operations.AsReadOnly();

    private Account(Guid customerId, decimal creditLimit)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        AvailableBalance = 0m;
        ReservedBalance = 0m;
        CreditLimit = creditLimit;
    }

    public static Account Open(Guid customerId, decimal creditLimit = 0m)
    {
        if (creditLimit < 0)
            throw new ArgumentException("Credit limit cannot be negative.");

        return new Account(customerId, creditLimit);
    }

    public void Credit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Credit amount must be greater than zero.", nameof(amount));

        AvailableBalance += amount;
        _operations.Add(new AccountOperation(Id, OperationType.Credit, amount));
    }

    public void Debit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Debit amount must be greater than zero.");

        var availableWithCreditLimit = AvailableBalance + CreditLimit;

        if (amount > availableWithCreditLimit)
            throw new InvalidOperationException("Insufficient funds to complete the debit.");

        AvailableBalance -= amount;
        _operations.Add(new AccountOperation(Id, OperationType.Debit, amount));
    }

    public void Reserve(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Reserve amount must be greater than zero.", nameof(amount));

        if (amount > AvailableBalance)
            throw new InvalidOperationException($"Insufficient available balance. Available: {AvailableBalance}, Requested: {amount}");

        AvailableBalance -= amount;
        ReservedBalance += amount;
        _operations.Add(new AccountOperation(Id, OperationType.Reserve, amount));
    }

    public void Capture(Guid reserveOperationId)
    {
        var reserveOperation = _operations.FirstOrDefault(o =>
            o.Id == reserveOperationId && o.Type == OperationType.Reserve);

        if (reserveOperation is null)
            throw new InvalidOperationException($"Reserve operation {reserveOperationId} not found.");

        var amount = reserveOperation.Amount;

        if (amount > ReservedBalance)
            throw new InvalidOperationException($"Insufficient reserved balance. Reserved: {ReservedBalance}, Requested: {amount}");

        ReservedBalance -= amount;
        _operations.Add(new AccountOperation(Id, OperationType.Capture, amount));
    }

    public void Reversal(Guid originalOperationId)
    {
        var originalOperation = _operations.FirstOrDefault(o => o.Id == originalOperationId);

        if (originalOperation is null)
            throw new InvalidOperationException($"Operation {originalOperationId} not found.");

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
                throw new InvalidOperationException($"Operations of type '{originalOperation.Type}' cannot be reversed.");
        }

        _operations.Add(new AccountOperation(Id, OperationType.Reversal, amount));
    }
}
