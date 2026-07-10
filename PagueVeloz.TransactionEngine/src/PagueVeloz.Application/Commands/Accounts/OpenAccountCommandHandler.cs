using MediatR;
using PagueVeloz.Application.DTOs.Accounts.Responses;
using PagueVeloz.Application.Exceptions;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Commands.Accounts;

public class OpenAccountCommandHandler : IRequestHandler<OpenAccountCommand, AccountResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OpenAccountCommandHandler(IAccountRepository accountRepository, ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AccountResponse> Handle(OpenAccountCommand request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        var account = Account.Open(customer.Id, request.CreditLimit);
        await _accountRepository.AddAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return AccountResponse.From(account);
    }
}
