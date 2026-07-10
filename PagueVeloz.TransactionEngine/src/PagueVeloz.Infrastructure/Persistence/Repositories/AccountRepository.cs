using Microsoft.EntityFrameworkCore;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces;
using PagueVeloz.Infrastructure.Persistence.Context;

namespace PagueVeloz.Infrastructure.Persistence.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly AppDbContext _context;

    public AccountRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Account?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .Include(a => a.Operations.OrderByDescending(o => o.OccurredAt))
            .FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
    }

    public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        await _context.Accounts.AddAsync(account, cancellationToken);
    }

    public async Task<IEnumerable<AccountOperation>> GetOperationsByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default)
    {
        return await _context.AccountOperations
            .Where(o => o.ReferenceId == referenceId)
            .ToListAsync(cancellationToken);
    }
}
