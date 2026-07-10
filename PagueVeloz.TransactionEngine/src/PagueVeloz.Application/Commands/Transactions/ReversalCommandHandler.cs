using MediatR;
using PagueVeloz.Application.DTOs.Responses;
using PagueVeloz.Application.Exceptions;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Enums;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Commands.Transactions;

public class ReversalCommandHandler : IRequestHandler<ReversalCommand, TransactionResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReversalCommandHandler(IAccountRepository accountRepository, IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TransactionResponse> Handle(ReversalCommand request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.AccountId);

        var originalOperation = account.Operations.FirstOrDefault(o => o.Id == request.OriginalOperationId);
        var operation = account.Reversal(request.OriginalOperationId, request.ReferenceId, request.Currency, request.Metadata);

        if (originalOperation is not null && operation.Status == OperationStatus.Success)
        {
            var allOperations = await _accountRepository.GetOperationsByReferenceIdAsync(originalOperation.ReferenceId);
            var pairedOperation = allOperations.FirstOrDefault(o => o.AccountId != request.AccountId);

            if (pairedOperation is not null)
            {
                var otherAccount = await _accountRepository.GetByIdAsync(pairedOperation.AccountId)
                    ?? throw new NotFoundException(nameof(Account), pairedOperation.AccountId);

                otherAccount.Reversal(pairedOperation.Id, request.ReferenceId, request.Currency, request.Metadata);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return TransactionResponse.From(account, operation);
    }
}
