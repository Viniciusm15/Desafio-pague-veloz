using MediatR;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Queries.Accounts;

public class GetAccountQueryHandler : IRequestHandler<GetAccountQuery, Account?>
{
    private readonly IAccountRepository _accountRepository;

    public GetAccountQueryHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<Account?> Handle(GetAccountQuery request, CancellationToken cancellationToken)
    {
        return await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
    }
}
