using MediatR;
using Microsoft.Extensions.Logging;
using PagueVeloz.Application.DTOs.Transactions.Responses;
using PagueVeloz.Application.Exceptions;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Enums;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Commands.Transactions;

public class ReversalCommandHandler : IRequestHandler<ReversalCommand, TransactionResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReversalCommandHandler> _logger;

    public ReversalCommandHandler(
        IAccountRepository accountRepository,
        IUnitOfWork unitOfWork,
        ILogger<ReversalCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TransactionResponse> Handle(ReversalCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing reversal operation. AccountId {AccountId}, OriginalOperationId {OriginalOperationId}, ReferenceId {ReferenceId}",
            request.AccountId,
            request.OriginalOperationId,
            request.ReferenceId);

        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.AccountId);

        var originalOperation = account.Operations.FirstOrDefault(o => o.Id == request.OriginalOperationId);
        var operation = account.Reversal(request.OriginalOperationId, request.ReferenceId, request.Currency, request.Metadata);

        if (originalOperation is not null && operation.Status == OperationStatus.Success)
        {
            var allOperations = await _accountRepository.GetOperationsByReferenceIdAsync(originalOperation.ReferenceId, cancellationToken);
            var pairedOperation = allOperations.FirstOrDefault(o => o.AccountId != request.AccountId);

            if (pairedOperation is not null)
            {
                _logger.LogInformation(
                    "Reversal includes paired operation for transfer. PairedAccountId {PairedAccountId}, PairedOperationId {PairedOperationId}",
                    pairedOperation.AccountId,
                    pairedOperation.Id);

                var otherAccount = await _accountRepository.GetByIdAsync(pairedOperation.AccountId, cancellationToken)
                    ?? throw new NotFoundException(nameof(Account), pairedOperation.AccountId);

                otherAccount.Reversal(pairedOperation.Id, request.ReferenceId, request.Currency, request.Metadata);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Reversal operation completed successfully. AccountId {AccountId}, OperationId {OperationId}, NewBalance {NewBalance}",
            request.AccountId,
            operation.Id,
            account.AvailableBalance + account.ReservedBalance);

        return TransactionResponse.From(account, operation);
    }
}
