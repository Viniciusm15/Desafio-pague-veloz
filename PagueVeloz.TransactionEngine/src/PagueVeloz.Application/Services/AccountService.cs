using PagueVeloz.Application.DTOs.Requests.Account;
using PagueVeloz.Application.Exceptions;
using PagueVeloz.Application.Interfaces;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Enums;
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

    public async Task<(Account Account, AccountOperation Operation)> CreditAsync(Guid accountId, CreditAccountRequest request)
    {
        var account = await GetAccountByIdAsync(accountId);

        var operation = account.Credit(request.Amount, request.ReferenceId, request.Currency, request.Metadata);
        await _unitOfWork.SaveChangesAsync();

        return (account, operation);
    }

    public async Task<(Account Account, AccountOperation Operation)> DebitAsync(Guid accountId, DebitAccountRequest request)
    {
        var account = await GetAccountByIdAsync(accountId);

        var operation = account.Debit(request.Amount, request.ReferenceId, request.Currency, request.Metadata);
        await _unitOfWork.SaveChangesAsync();

        return (account, operation);
    }

    public async Task<(Account Account, AccountOperation Operation)> ReserveAsync(Guid accountId, ReserveAccountRequest request)
    {
        var account = await GetAccountByIdAsync(accountId);

        var operation = account.Reserve(request.Amount, request.ReferenceId, request.Currency, request.Metadata);
        await _unitOfWork.SaveChangesAsync();

        return (account, operation);
    }

    public async Task<(Account Account, AccountOperation Operation)> CaptureAsync(Guid accountId, CaptureAccountRequest request)
    {
        var account = await GetAccountByIdAsync(accountId);

        var operation = account.Capture(request.ReserveOperationId, request.ReferenceId, request.Currency, request.Metadata);
        await _unitOfWork.SaveChangesAsync();

        return (account, operation);
    }

    public async Task<(Account Account, AccountOperation Operation, Account? OtherAccount, AccountOperation? OtherOperation)> ReversalAsync(Guid accountId, ReversalAccountRequest request)
    {
        var account = await GetAccountByIdAsync(accountId);

        var originalOperation = account.Operations.FirstOrDefault(o => o.Id == request.OriginalOperationId);
        Account? otherAccount = null;
        AccountOperation? pairedOperation = null;

        if (originalOperation is not null)
        {
            var allOperations = await _accountRepository.GetOperationsByReferenceIdAsync(originalOperation.ReferenceId);
            pairedOperation = allOperations.FirstOrDefault(o => o.AccountId != accountId);

            if (pairedOperation is not null)
                otherAccount = await GetAccountByIdAsync(pairedOperation.AccountId);
        }

        var operation = account.Reversal(request.OriginalOperationId, request.ReferenceId, request.Currency, request.Metadata);

        AccountOperation? otherOperation = null;
        if (otherAccount is not null && pairedOperation is not null && operation.Status == OperationStatus.Success)
            otherOperation = otherAccount.Reversal(pairedOperation.Id, request.ReferenceId, request.Currency, request.Metadata);

        await _unitOfWork.SaveChangesAsync();

        return (account, operation, otherAccount, otherOperation);
    }

    public async Task<(Account Source, AccountOperation SourceOperation, Account Destination, AccountOperation DestinationOperation)> TransferAsync(TransferAccountRequest request)
    {
        if (request.SourceAccountId == request.DestinationAccountId)
            throw new ArgumentException("Source and destination accounts must be different.");

        var source = await GetAccountByIdAsync(request.SourceAccountId);
        var destination = await GetAccountByIdAsync(request.DestinationAccountId);

        var sourceOperation = source.Debit(request.Amount, request.ReferenceId, request.Currency, request.Metadata);

        AccountOperation destinationOperation;
        if (sourceOperation.Status == OperationStatus.Success)
        {
            destinationOperation = destination.Credit(request.Amount, request.ReferenceId, request.Currency, request.Metadata);
        }
        else
        {
            destinationOperation = AccountOperation.Failed(
                destination.Id, OperationType.Credit, request.Amount, request.Currency,
                request.ReferenceId, "Transfer aborted: source debit failed.", request.Metadata);
        }

        await _unitOfWork.SaveChangesAsync();

        return (source, sourceOperation, destination, destinationOperation);
    }

    #region Private Methods

    private async Task<Account> GetAccountByIdAsync(Guid accountId)
    {
        var account = await _accountRepository.GetByIdAsync(accountId);
        return account ?? throw new NotFoundException(nameof(Account), accountId);
    }

    #endregion
}
