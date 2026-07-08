using PagueVeloz.Application.DTOs.Requests;
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
        Task<Account> CreditAsync(Guid accountId, CreditAccountRequest request);
        Task<Account> DebitAsync(Guid accountId, DebitAccountRequest request);
        Task<Account> ReserveAsync(Guid accountId, ReserveAccountRequest request);
        Task<Account> CaptureAsync(Guid accountId, CaptureAccountRequest request);
        Task<Account> ReversalAsync(Guid accountId, ReversalAccountRequest request);
        Task<(Account Source, Account Destination)> TransferAsync(TransferRequest request);
    }
}
