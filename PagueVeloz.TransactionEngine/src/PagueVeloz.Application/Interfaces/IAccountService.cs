using PagueVeloz.Application.DTOs.Requests.Account;
using PagueVeloz.Domain.Entities;

namespace PagueVeloz.Application.Interfaces
{
    public interface IAccountService
    {
        Task<Account> OpenAccountAsync(CreateAccountRequest request);
        Task<Account?> GetByIdAsync(Guid accountId);
        Task<Account> BlockAsync(Guid accountId);
        Task<Account> ReactivateAsync(Guid accountId);
        Task<Account> DeactivateAsync(Guid accountId);
        Task<(Account Account, AccountOperation Operation)> CreditAsync(Guid accountId, CreditAccountRequest request);
        Task<(Account Account, AccountOperation Operation)> DebitAsync(Guid accountId, DebitAccountRequest request);
        Task<(Account Account, AccountOperation Operation)> ReserveAsync(Guid accountId, ReserveAccountRequest request);
        Task<(Account Account, AccountOperation Operation)> CaptureAsync(Guid accountId, CaptureAccountRequest request);
        Task<(Account Account, AccountOperation Operation)> ReversalAsync(Guid accountId, ReversalAccountRequest request);
        Task<(Account Account, AccountOperation Operation)> TransferAsync(TransferAccountRequest request);
    }
}
