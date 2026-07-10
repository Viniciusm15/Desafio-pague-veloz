using PagueVeloz.Domain.Entities;

namespace PagueVeloz.Domain.Interfaces;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task AddAsync(Account account, CancellationToken cancellationToken = default);
    Task<IEnumerable<AccountOperation>> GetOperationsByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default);
}
