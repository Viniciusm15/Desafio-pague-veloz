using PagueVeloz.Domain.Entities;

namespace PagueVeloz.Application.Interfaces;

public interface IAccountService
{
    Task<Account> OpenAccountAsync(Guid customerId);
    Task<Account?> GetByIdAsync(Guid accountId);
    Task<Account> CreditAsync(Guid accountId, decimal amount);
    Task<Account> DebitAsync(Guid accountId, decimal amount);
    Task<Account> ReserveAsync(Guid accountId, decimal amount);
    Task<Account> CaptureAsync(Guid accountId, Guid reserveOperationId);
    Task<Account> ReversalAsync(Guid accountId, Guid originalOperationId);
    Task<(Account Source, Account Destination)> TransferAsync(Guid sourceAccountId, Guid destinationAccountId, decimal amount);
}
