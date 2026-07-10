using MediatR;
using PagueVeloz.Application.DTOs.Accounts.Responses;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Queries.Accounts;

public class GetAccountQueryHandler : IRequestHandler<GetAccountQuery, AccountResponse?>
{
    private readonly IAccountRepository _accountRepository;

    public GetAccountQueryHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<AccountResponse?> Handle(GetAccountQuery request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        return account is null ? null : AccountResponse.From(account);
    }
}
