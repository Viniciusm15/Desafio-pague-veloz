using MediatR;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<OpenAccountCommandHandler> _logger;

    public OpenAccountCommandHandler(
        IAccountRepository accountRepository,
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        ILogger<OpenAccountCommandHandler> logger)
    {
        _accountRepository = accountRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AccountResponse> Handle(OpenAccountCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Opening account. CustomerId {CustomerId}, CreditLimit {CreditLimit}",
            request.CustomerId,
            request.CreditLimit);

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        var account = Account.Open(customer.Id, request.CreditLimit);
        await _accountRepository.AddAsync(account, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Account opened successfully. AccountId {AccountId}, CustomerId {CustomerId}, CreditLimit {CreditLimit}",
            account.Id,
            customer.Id,
            account.CreditLimit);

        return AccountResponse.From(account);
    }
}
