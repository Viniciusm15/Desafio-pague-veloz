using PagueVeloz.Application.Exceptions;
using PagueVeloz.Application.Interfaces;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ICustomerRepository _customerRepository;

    public AccountService(IAccountRepository accountRepository, ICustomerRepository customerRepository)
    {
        _accountRepository = accountRepository;
        _customerRepository = customerRepository;
    }

    public async Task<Account> OpenAccountAsync(Guid customerId)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId)
            ?? throw new NotFoundException(nameof(Customer), customerId);

        var account = Account.Open(customer.Id);
        await _accountRepository.AddAsync(account);

        return account;
    }

    public Task<Account?> GetByIdAsync(Guid accountId)
    {
        return _accountRepository.GetByIdAsync(accountId);
    }

    public async Task<Account> CreditAsync(Guid accountId, decimal amount)
    {
        var account = await _accountRepository.GetByIdAsync(accountId)
            ?? throw new NotFoundException(nameof(Account), accountId);

        account.Credit(amount);
        await _accountRepository.UpdateAsync(account);

        return account;
    }

    public async Task<Account> DebitAsync(Guid accountId, decimal amount)
    {
        var account = await _accountRepository.GetByIdAsync(accountId)
            ?? throw new NotFoundException(nameof(Account), accountId);

        account.Debit(amount);
        await _accountRepository.UpdateAsync(account);

        return account;
    }

    public async Task<Account> ReserveAsync(Guid accountId, decimal amount)
    {
        var account = await _accountRepository.GetByIdAsync(accountId)
            ?? throw new NotFoundException(nameof(Account), accountId);

        account.Reserve(amount);
        await _accountRepository.UpdateAsync(account);

        return account;
    }
}
