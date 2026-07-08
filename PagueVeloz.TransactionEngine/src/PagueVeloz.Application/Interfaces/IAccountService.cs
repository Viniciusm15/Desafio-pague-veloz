using PagueVeloz.Domain.Entities;

namespace PagueVeloz.Application.Interfaces
{
    public interface IAccountService
    {
        Task<Account> OpenAccountAsync(Guid customerId, decimal creditLimit = 0m);
        Task<Account?> GetByIdAsync(Guid accountId);
        Task<Account> BlockAsync(Guid accountId);
        Task<Account> ReactivateAsync(Guid accountId);
        Task<Account> DeactivateAsync(Guid accountId);
        Task<Account> CreditAsync(Guid accountId, decimal amount, string referenceId);
        Task<Account> DebitAsync(Guid accountId, decimal amount, string referenceId);
        Task<Account> ReserveAsync(Guid accountId, decimal amount, string referenceId);
        Task<Account> CaptureAsync(Guid accountId, Guid reserveOperationId, string referenceId);
        Task<Account> ReversalAsync(Guid accountId, Guid originalOperationId, string referenceId);
        Task<(Account Source, Account Destination)> TransferAsync(Guid sourceAccountId, Guid destinationAccountId, decimal amount, string referenceId);
    }
}
