using PagueVeloz.Domain.Enums;

namespace PagueVeloz.Domain.Entities;

public class AccountOperation
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public OperationType Type { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime OccurredAt { get; private set; }

    private AccountOperation() { }

    public AccountOperation(Guid accountId, OperationType type, decimal amount)
    {
        Id = Guid.NewGuid();
        AccountId = accountId;
        Type = type;
        Amount = amount;
        OccurredAt = DateTime.UtcNow;
    }
}
