using MediatR;
using Microsoft.Extensions.Logging;
using PagueVeloz.Application.DTOs.Transactions.Responses;
using PagueVeloz.Application.Exceptions;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Commands.Transactions;

public class CaptureCommandHandler : IRequestHandler<CaptureCommand, TransactionResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CaptureCommandHandler> _logger;

    public CaptureCommandHandler(
        IAccountRepository accountRepository,
        IUnitOfWork unitOfWork,
        ILogger<CaptureCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<TransactionResponse> Handle(
        CaptureCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing capture operation. AccountId {AccountId}, ReserveOperationId {ReserveOperationId}, ReferenceId {ReferenceId}",
            request.AccountId,
            request.ReserveOperationId,
            request.ReferenceId);

        var account = await _accountRepository.GetByIdAsync(
            request.AccountId,
            cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.AccountId);

        var operation = account.Capture(
            request.ReserveOperationId,
            request.ReferenceId,
            request.Currency,
            request.Metadata);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Capture operation completed successfully. AccountId {AccountId}, OperationId {OperationId}",
            request.AccountId,
            operation.Id);

        return TransactionResponse.From(account, operation);
    }
}
