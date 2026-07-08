using PagueVeloz.Application.DTOs.Requests.Account;
using PagueVeloz.Application.Exceptions;
using PagueVeloz.Application.Interfaces;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AccountService(
        IAccountRepository accountRepository,
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork)
    {
        _accountRepository = accountRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Account> OpenAccountAsync(CreateAccountRequest request)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        var account = Account.Open(customer.Id, request.CreditLimit);
        await _accountRepository.AddAsync(account);

        return account;
    }

    public Task<Account?> GetByIdAsync(Guid accountId)
    {
        return _accountRepository.GetByIdAsync(accountId);
    }

    public async Task<Account> BlockAsync(Guid accountId)
    {
        var account = await GetAccountByIdAsync(accountId);

        account.Block();
        await _unitOfWork.SaveChangesAsync();

        return account;
    }

    public async Task<Account> ReactivateAsync(Guid accountId)
    {
        var account = await GetAccountByIdAsync(accountId);

        account.Activate();
        await _unitOfWork.SaveChangesAsync();

        return account;
    }

    public async Task<Account> DeactivateAsync(Guid accountId)
    {
        var account = await GetAccountByIdAsync(accountId);

        account.Deactivate();
        await _unitOfWork.SaveChangesAsync();

        return account;
    }

    public async Task<Account> CreditAsync(Guid accountId, CreditAccountRequest request)
    {
        var account = await GetAccountByIdAsync(accountId);

        account.Credit(request.Amount, request.ReferenceId, request.Currency, request.Metadata);
        await _unitOfWork.SaveChangesAsync();

        return account;
    }

    public async Task<Account> DebitAsync(Guid accountId, DebitAccountRequest request)
    {
        var account = await GetAccountByIdAsync(accountId);

        account.Debit(request.Amount, request.ReferenceId, request.Currency, request.Metadata);
        await _unitOfWork.SaveChangesAsync();

        return account;
    }

    public async Task<Account> ReserveAsync(Guid accountId, ReserveAccountRequest request)
    {
        var account = await GetAccountByIdAsync(accountId);

        account.Reserve(request.Amount, request.ReferenceId, request.Currency, request.Metadata);
        await _unitOfWork.SaveChangesAsync();

        return account;
    }

    public async Task<Account> CaptureAsync(Guid accountId, CaptureAccountRequest request)
    {
        var account = await GetAccountByIdAsync(accountId);

        account.Capture(request.ReserveOperationId, request.ReferenceId, request.Currency, request.Metadata);
        await _unitOfWork.SaveChangesAsync();

        return account;
    }

    public async Task<Account> ReversalAsync(Guid accountId, ReversalAccountRequest request)
    {
        var account = await GetAccountByIdAsync(accountId);
        var originalOperation = account.Operations.FirstOrDefault(o => o.Id == request.OriginalOperationId)
            ?? throw new InvalidOperationException($"Operation {request.OriginalOperationId} not found.");

        var allOperations = await _accountRepository.GetOperationsByReferenceIdAsync(originalOperation.ReferenceId);
        var otherOperation = allOperations.FirstOrDefault(o => o.AccountId != accountId);

        if (otherOperation is not null)
        {
            var otherAccount = await GetAccountByIdAsync(otherOperation.AccountId);

            account.Reversal(request.OriginalOperationId, request.ReferenceId, request.Currency, request.Metadata);
            otherAccount.Reversal(otherOperation.Id, request.ReferenceId, request.Currency, request.Metadata);
            await _unitOfWork.SaveChangesAsync();

            return account;
        }

        account.Reversal(request.OriginalOperationId, request.ReferenceId, request.Currency, request.Metadata);
        await _unitOfWork.SaveChangesAsync();
        return account;
    }

    public async Task<(Account Source, Account Destination)> TransferAsync(TransferAccountRequest request)
    {
        if (request.SourceAccountId == request.DestinationAccountId)
            throw new ArgumentException("Source and destination accounts must be different.");

        var source = await GetAccountByIdAsync(request.SourceAccountId);
        var destination = await GetAccountByIdAsync(request.DestinationAccountId);

        source.Debit(request.Amount, request.ReferenceId, request.Currency, request.Metadata);
        destination.Credit(request.Amount, request.ReferenceId, request.Currency, request.Metadata);

        await _unitOfWork.SaveChangesAsync();

        return (source, destination);
    }

    #region Private Methods

    private async Task<Account> GetAccountByIdAsync(Guid accountId)
    {
        var account = await _accountRepository.GetByIdAsync(accountId);
        return account ?? throw new NotFoundException(nameof(Account), accountId);
    }

    #endregion
}
