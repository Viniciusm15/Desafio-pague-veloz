using MediatR;
using Microsoft.Extensions.Logging;
using PagueVeloz.Application.DTOs.Accounts.Responses;
using PagueVeloz.Application.Exceptions;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Commands.Accounts;

public class ReactivateAccountCommandHandler : IRequestHandler<ReactivateAccountCommand, AccountResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReactivateAccountCommandHandler> _logger;

    public ReactivateAccountCommandHandler(
        IAccountRepository accountRepository,
        IUnitOfWork unitOfWork,
        ILogger<ReactivateAccountCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AccountResponse> Handle(ReactivateAccountCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Reactivating account. AccountId {AccountId}",
            request.AccountId);

        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.AccountId);

        account.Activate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Account reactivated successfully. AccountId {AccountId}, NewStatus {NewStatus}",
            account.Id,
            account.Status);

        return AccountResponse.From(account);
    }
}
