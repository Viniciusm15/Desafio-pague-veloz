using MediatR;
using Microsoft.Extensions.Logging;
using PagueVeloz.Application.DTOs.Accounts.Responses;
using PagueVeloz.Application.Exceptions;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Commands.Accounts;

public class BlockAccountCommandHandler : IRequestHandler<BlockAccountCommand, AccountResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BlockAccountCommandHandler> _logger;

    public BlockAccountCommandHandler(
        IAccountRepository accountRepository,
        IUnitOfWork unitOfWork,
        ILogger<BlockAccountCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AccountResponse> Handle(BlockAccountCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Blocking account. AccountId {AccountId}",
            request.AccountId);

        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.AccountId);

        account.Block();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Account blocked successfully. AccountId {AccountId}, NewStatus {NewStatus}",
            account.Id,
            account.Status);

        return AccountResponse.From(account);
    }
}
