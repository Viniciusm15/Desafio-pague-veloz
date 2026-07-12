using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PagueVeloz.Application.Commands.Transactions;
using PagueVeloz.Application.Exceptions;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Enums;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Tests.Commands.Transactions;

public class ReversalCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<ReversalCommandHandler>> _loggerMock;
    private readonly ReversalCommandHandler _handler;

    public ReversalCommandHandlerTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<ReversalCommandHandler>>();

        _handler = new ReversalCommandHandler(
            _accountRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldReverseOperation_WhenOriginalOperationExists()
    {
        var account = CreateActiveAccountWithBalance(500);
        var debitOperation = account.Debit(200, "REF-DEBIT-001", "BRL", null);
        var command = new ReversalCommand(
            account.Id,
            debitOperation.Id,
            "REF-REVERSAL-001",
            "BRL",
            null);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _accountRepositoryMock
            .Setup(repo => repo.GetOperationsByReferenceIdAsync("REF-DEBIT-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AccountOperation>());

        var result = await _handler.Handle(command, CancellationToken.None);

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        result.Should().NotBeNull();
        result.Status.Should().Be(OperationStatus.Success);
        result.Balance.Should().Be(500);
        result.ReservedBalance.Should().Be(0);
        result.AvailableBalance.Should().Be(500);
        result.ErrorMessage.Should().BeNull();

        VerifyLoggerInformation("Processing reversal operation", Times.Once());
        VerifyLoggerInformation("Reversal operation completed successfully", Times.Once());
        VerifyLoggerInformation("Reversal includes paired operation for transfer", Times.Never());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailedOperation_WhenOriginalOperationNotFound()
    {
        var account = CreateActiveAccount();
        var originalId = Guid.NewGuid();
        var command = new ReversalCommand(
            account.Id,
            originalId,
            "REF-REVERSAL-002",
            "BRL",
            null);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var result = await _handler.Handle(command, CancellationToken.None);

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        result.Should().NotBeNull();
        result.Status.Should().Be(OperationStatus.Failed);
        result.ErrorMessage.Should().Contain("not found");

        VerifyLoggerInformation("Processing reversal operation", Times.Once());
        VerifyLoggerInformation("Reversal operation completed successfully", Times.Once());
        VerifyLoggerInformation("Reversal includes paired operation for transfer", Times.Never());
    }

    [Fact]
    public async Task Handle_ShouldReversePairedOperation_WhenOriginalOperationIsTransfer()
    {
        var sourceAccount = CreateActiveAccountWithBalance(500);
        var destinationAccount = CreateActiveAccount();

        var debitOperation = sourceAccount.Debit(200, "REF-TRANSFER-001", "BRL", null);
        var creditOperation = destinationAccount.Credit(200, "REF-TRANSFER-001", "BRL", null);

        var command = new ReversalCommand(
            sourceAccount.Id,
            debitOperation.Id,
            "REF-REVERSAL-003",
            "BRL",
            null);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(sourceAccount.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceAccount);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(destinationAccount.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(destinationAccount);

        _accountRepositoryMock
            .Setup(repo => repo.GetOperationsByReferenceIdAsync("REF-TRANSFER-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AccountOperation> { debitOperation, creditOperation });

        var result = await _handler.Handle(command, CancellationToken.None);

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        result.Should().NotBeNull();
        result.Status.Should().Be(OperationStatus.Success);
        result.Balance.Should().Be(500);
        result.ReservedBalance.Should().Be(0);
        result.AvailableBalance.Should().Be(500);
        result.ErrorMessage.Should().BeNull();

        destinationAccount.Operations.Should().Contain(o => o.Type == OperationType.Reversal);
        destinationAccount.AvailableBalance.Should().Be(0);

        VerifyLoggerInformation("Processing reversal operation", Times.Once());
        VerifyLoggerInformation("Reversal operation completed successfully", Times.Once());
        VerifyLoggerInformation("Reversal includes paired operation for transfer", Times.Once());
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenAccountDoesNotExist()
    {
        var accountId = Guid.NewGuid();
        var command = new ReversalCommand(accountId, Guid.NewGuid(), "REF-123", "BRL", null);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account)null!);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage($"*{nameof(Account)}*{accountId}*");

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        VerifyLoggerInformation("Processing reversal operation", Times.Once());
        VerifyLoggerInformation("Reversal operation completed successfully", Times.Never());
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenPairedAccountDoesNotExist()
    {
        var sourceAccount = CreateActiveAccountWithBalance(500);
        var destinationAccount = CreateActiveAccount();

        var debitOperation = sourceAccount.Debit(200, "REF-TRANSFER-002", "BRL", null);
        var creditOperation = destinationAccount.Credit(200, "REF-TRANSFER-002", "BRL", null);

        var command = new ReversalCommand(
            sourceAccount.Id,
            debitOperation.Id,
            "REF-REVERSAL-004",
            "BRL",
            null);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(sourceAccount.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceAccount);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(destinationAccount.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account)null!);

        _accountRepositoryMock
            .Setup(repo => repo.GetOperationsByReferenceIdAsync("REF-TRANSFER-002", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AccountOperation> { debitOperation, creditOperation });

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage($"*{nameof(Account)}*{destinationAccount.Id}*");

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        VerifyLoggerInformation("Processing reversal operation", Times.Once());
        VerifyLoggerInformation("Reversal includes paired operation for transfer", Times.Once());
        VerifyLoggerInformation("Reversal operation completed successfully", Times.Never());
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenSaveChangesFails()
    {
        var account = CreateActiveAccountWithBalance(500);
        var debitOperation = account.Debit(200, "REF-DEBIT-002", "BRL", null);
        var command = new ReversalCommand(
            account.Id,
            debitOperation.Id,
            "REF-REVERSAL-005",
            "BRL",
            null);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _accountRepositoryMock
            .Setup(repo => repo.GetOperationsByReferenceIdAsync("REF-DEBIT-002", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AccountOperation>());

        _unitOfWorkMock
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Database error");

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        VerifyLoggerInformation("Processing reversal operation", Times.Once());
        VerifyLoggerInformation("Reversal operation completed successfully", Times.Never());
    }

    private static Account CreateActiveAccount()
    {
        var customerId = Guid.NewGuid();
        return Account.Open(customerId, creditLimit: 0);
    }

    private static Account CreateActiveAccountWithBalance(long amount)
    {
        var account = CreateActiveAccount();
        account.Credit(amount, "INITIAL", "BRL", null);
        return account;
    }

    private void VerifyLoggerInformation(string expectedMessage, Times times)
    {
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            times);
    }
}
