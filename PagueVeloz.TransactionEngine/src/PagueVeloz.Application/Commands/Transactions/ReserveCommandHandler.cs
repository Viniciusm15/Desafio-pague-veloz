using MediatR;
using Microsoft.Extensions.Logging;
using PagueVeloz.Application.DTOs.Transactions.Responses;
using PagueVeloz.Application.Exceptions;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Commands.Transactions;

public class ReserveCommandHandler : IRequestHandler<ReserveCommand, TransactionResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReserveCommandHandler> _logger;

    public ReserveCommandHandler(
        IAccountRepository accountRepository,
        IUnitOfWork unitOfWork,
        ILogger<ReserveCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TransactionResponse> Handle(ReserveCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing reserve operation. AccountId {AccountId}, Amount {Amount}, ReferenceId {ReferenceId}",
            request.AccountId,
            request.Amount,
            request.ReferenceId);

        var account = await _accountRepository.GetByIdAsync(
            request.AccountId,
            cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.AccountId);

        var operation = account.Reserve(
            request.Amount,
            request.ReferenceId,
            request.Currency,
            request.Metadata);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Reserve operation completed successfully. AccountId {AccountId}, OperationId {OperationId}, ReservedBalance {ReservedBalance}",
            request.AccountId,
            operation.Id,
            account.ReservedBalance);

        return TransactionResponse.From(account, operation);
    }
}
