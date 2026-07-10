using MediatR;
using PagueVeloz.Application.DTOs.Responses;
using PagueVeloz.Application.Exceptions;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Commands.Transactions;

public class ReserveCommandHandler : IRequestHandler<ReserveCommand, TransactionResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReserveCommandHandler(IAccountRepository accountRepository, IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TransactionResponse> Handle(ReserveCommand request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.AccountId);

        var operation = account.Reserve(request.Amount, request.ReferenceId, request.Currency, request.Metadata);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return TransactionResponse.From(account, operation);
    }
}
