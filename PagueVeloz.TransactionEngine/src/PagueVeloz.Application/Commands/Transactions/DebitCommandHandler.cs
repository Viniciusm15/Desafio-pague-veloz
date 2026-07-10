using MediatR;
using PagueVeloz.Application.DTOs.Responses;
using PagueVeloz.Application.Exceptions;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Commands.Transactions;

public class DebitCommandHandler : IRequestHandler<DebitCommand, TransactionResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DebitCommandHandler(IAccountRepository accountRepository, IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TransactionResponse> Handle(DebitCommand request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.AccountId);

        var operation = account.Debit(request.Amount, request.ReferenceId, request.Currency, request.Metadata);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return TransactionResponse.From(account, operation);
    }
}
