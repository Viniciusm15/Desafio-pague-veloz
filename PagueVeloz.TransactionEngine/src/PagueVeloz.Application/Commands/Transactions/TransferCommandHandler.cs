using MediatR;
using Microsoft.Extensions.Logging;
using PagueVeloz.Application.DTOs.Transactions.Responses;
using PagueVeloz.Application.Exceptions;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Commands.Transactions;

public class TransferCommandHandler : IRequestHandler<TransferCommand, TransactionResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TransferCommandHandler> _logger;

    public TransferCommandHandler(
        IAccountRepository accountRepository,
        IUnitOfWork unitOfWork,
        ILogger<TransferCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TransactionResponse> Handle(TransferCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing transfer operation. SourceAccountId {SourceAccountId}, DestinationAccountId {DestinationAccountId}, Amount {Amount}, " +
            "ReferenceId {ReferenceId}",
            request.SourceAccountId,
            request.DestinationAccountId,
            request.Amount,
            request.ReferenceId);

        if (request.SourceAccountId == request.DestinationAccountId)
            throw new ArgumentException("Source and destination accounts must be different.");

        var source = await _accountRepository.GetByIdAsync(request.SourceAccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.SourceAccountId);

        var destination = await _accountRepository.GetByIdAsync(request.DestinationAccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.DestinationAccountId);

        var sourceOp = source.Debit(request.Amount, request.ReferenceId, request.Currency, request.Metadata);
        var destOp = destination.Credit(request.Amount, request.ReferenceId, request.Currency, request.Metadata);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Transfer operation completed successfully. SourceAccountId {SourceAccountId}, DestinationAccountId {DestinationAccountId}, SourceOperationId " +
            "{SourceOperationId}, DestinationOperationId {DestinationOperationId}, SourceNewBalance {SourceNewBalance}, DestinationNewBalance {DestinationNewBalance}",
            source.Id,
            destination.Id,
            sourceOp.Id,
            destOp.Id,
            source.AvailableBalance + source.ReservedBalance,
            destination.AvailableBalance + destination.ReservedBalance);

        return TransactionResponse.From(source, sourceOp);
    }
}
