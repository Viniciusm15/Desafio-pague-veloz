using MediatR;
using Microsoft.Extensions.Logging;
using PagueVeloz.Application.DTOs.Transactions.Responses;
using PagueVeloz.Application.Exceptions;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Commands.Transactions;

public class CreditCommandHandler : IRequestHandler<CreditCommand, TransactionResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreditCommandHandler> _logger;

    public CreditCommandHandler(
        IAccountRepository accountRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreditCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TransactionResponse> Handle(
        CreditCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing credit operation. AccountId {AccountId}, Amount {Amount}, ReferenceId {ReferenceId}",
            request.AccountId,
            request.Amount,
            request.ReferenceId);

        var account = await _accountRepository.GetByIdAsync(
            request.AccountId,
            cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.AccountId);

        var operation = account.Credit(
            request.Amount,
            request.ReferenceId,
            request.Currency,
            request.Metadata);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Credit operation completed successfully. AccountId {AccountId}, OperationId {OperationId}",
            request.AccountId,
            operation.Id);

        return TransactionResponse.From(account, operation);
    }
}
