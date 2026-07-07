using PagueVeloz.Domain.Entities;

namespace PagueVeloz.Domain.Interfaces;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid accountId);
    Task AddAsync(Account account);
}
