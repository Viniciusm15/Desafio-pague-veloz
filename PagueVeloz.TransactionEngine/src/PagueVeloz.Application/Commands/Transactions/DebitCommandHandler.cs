using MediatR;
using Microsoft.Extensions.Logging;
using PagueVeloz.Application.DTOs.Transactions.Responses;
using PagueVeloz.Application.Exceptions;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Commands.Transactions;

public class DebitCommandHandler : IRequestHandler<DebitCommand, TransactionResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DebitCommandHandler> _logger;

    public DebitCommandHandler(
        IAccountRepository accountRepository,
        IUnitOfWork unitOfWork,
        ILogger<DebitCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TransactionResponse> Handle(
        DebitCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing debit operation. AccountId {AccountId}, Amount {Amount}, ReferenceId {ReferenceId}",
            request.AccountId,
            request.Amount,
            request.ReferenceId);

        var account = await _accountRepository.GetByIdAsync(
            request.AccountId,
            cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.AccountId);

        var operation = account.Debit(
            request.Amount,
            request.ReferenceId,
            request.Currency,
            request.Metadata);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Debit operation completed successfully. AccountId {AccountId}, OperationId {OperationId}",
            request.AccountId,
            operation.Id);

        return TransactionResponse.From(account, operation);
    }
}
