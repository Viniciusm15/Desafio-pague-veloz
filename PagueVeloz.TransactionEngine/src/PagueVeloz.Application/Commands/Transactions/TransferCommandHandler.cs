using MediatR;
using PagueVeloz.Application.DTOs.Responses;
using PagueVeloz.Application.Exceptions;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Commands.Transactions;

public class TransferCommandHandler : IRequestHandler<TransferCommand, TransactionResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TransferCommandHandler(IAccountRepository accountRepository, IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TransactionResponse> Handle(TransferCommand request, CancellationToken cancellationToken)
    {
        if (request.SourceAccountId == request.DestinationAccountId)
            throw new ArgumentException("Source and destination accounts must be different.");

        var source = await _accountRepository.GetByIdAsync(request.SourceAccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.SourceAccountId);

        var destination = await _accountRepository.GetByIdAsync(request.DestinationAccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.DestinationAccountId);

        var sourceOp = source.Debit(request.Amount, request.ReferenceId, request.Currency, request.Metadata);
        var destOp = destination.Credit(request.Amount, request.ReferenceId, request.Currency, request.Metadata);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return TransactionResponse.From(source, sourceOp);
    }
}
