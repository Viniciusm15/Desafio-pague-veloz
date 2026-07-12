using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PagueVeloz.Application.Commands.Transactions;
using PagueVeloz.Application.Exceptions;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Enums;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Tests.Commands.Transactions;

public class DebitCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<DebitCommandHandler>> _loggerMock;
    private readonly DebitCommandHandler _handler;

    public DebitCommandHandlerTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<DebitCommandHandler>>();

        _handler = new DebitCommandHandler(
            _accountRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldDebitAmount_WhenAccountExistsAndAmountIsValid()
    {
        var account = CreateActiveAccountWithBalance(500);
        var metadata = new Dictionary<string, object> { { "source", "test" } };
        var command = new DebitCommand(
            account.Id,
            200,
            "REF-DEBIT-001",
            "BRL",
            metadata);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var result = await _handler.Handle(command, CancellationToken.None);

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        result.Should().NotBeNull();
        result.Status.Should().Be(OperationStatus.Success);
        result.Balance.Should().Be(300);
        result.ReservedBalance.Should().Be(0);
        result.AvailableBalance.Should().Be(300);
        result.ErrorMessage.Should().BeNull();

        VerifyLoggerInformation("Processing debit operation", Times.Once());
        VerifyLoggerInformation("Debit operation completed successfully", Times.Once());
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenAccountDoesNotExist()
    {
        var accountId = Guid.NewGuid();
        var command = new DebitCommand(accountId, 100, "REF-123", "BRL", null);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account)null!);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage($"*{nameof(Account)}*{accountId}*");

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        VerifyLoggerInformation("Processing debit operation", Times.Once());
        VerifyLoggerInformation("Debit operation completed successfully", Times.Never());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailedOperation_WhenDebitFailsDueToInsufficientFunds()
    {
        var account = CreateActiveAccountWithBalance(50);
        var command = new DebitCommand(
            account.Id,
            100,
            "REF-DEBIT-002",
            "BRL",
            null);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var result = await _handler.Handle(command, CancellationToken.None);

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        result.Should().NotBeNull();
        result.Status.Should().Be(OperationStatus.Failed);
        result.ErrorMessage.Should().Contain("insufficient funds");

        VerifyLoggerInformation("Processing debit operation", Times.Once());
        VerifyLoggerInformation("Debit operation completed successfully", Times.Once());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailedOperation_WhenAccountIsInactive()
    {
        var account = CreateInactiveAccount();
        var command = new DebitCommand(
            account.Id,
            100,
            "REF-DEBIT-003",
            "BRL",
            null);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var result = await _handler.Handle(command, CancellationToken.None);

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        result.Should().NotBeNull();
        result.Status.Should().Be(OperationStatus.Failed);
        result.ErrorMessage.Should().ContainEquivalentOf("inactive");

        VerifyLoggerInformation("Processing debit operation", Times.Once());
        VerifyLoggerInformation("Debit operation completed successfully", Times.Once());
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenSaveChangesFails()
    {
        var account = CreateActiveAccountWithBalance(500);
        var command = new DebitCommand(
            account.Id,
            200,
            "REF-DEBIT-004",
            "BRL",
            null);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _unitOfWorkMock
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Database error");

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        VerifyLoggerInformation("Processing debit operation", Times.Once());
        VerifyLoggerInformation("Debit operation completed successfully", Times.Never());
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

    private static Account CreateInactiveAccount()
    {
        var account = CreateActiveAccount();
        account.Deactivate();
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
