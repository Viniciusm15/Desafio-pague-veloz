using MediatR;
using Microsoft.Extensions.Logging;
using PagueVeloz.Application.DTOs.Accounts.Responses;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Queries.Accounts;

public class GetAccountQueryHandler : IRequestHandler<GetAccountQuery, AccountResponse?>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ILogger<GetAccountQueryHandler> _logger;

    public GetAccountQueryHandler(
        IAccountRepository accountRepository,
        ILogger<GetAccountQueryHandler> logger)
    {
        _accountRepository = accountRepository;
        _logger = logger;
    }

    public async Task<AccountResponse?> Handle(GetAccountQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting account. AccountId {AccountId}", request.AccountId);

        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);

        if (account is null)
        {
            _logger.LogWarning("Account not found. AccountId {AccountId}", request.AccountId);
            return null;
        }

        _logger.LogInformation(
            "Account retrieved successfully. AccountId {AccountId}, Status {Status}, Balance {Balance}",
            account.Id,
            account.Status,
            account.AvailableBalance + account.ReservedBalance);

        return AccountResponse.From(account);
    }
}
