using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PagueVeloz.Application.Commands.Transactions;
using PagueVeloz.Application.Exceptions;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Enums;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Tests.Commands.Transactions;

public class TransferCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<TransferCommandHandler>> _loggerMock;
    private readonly TransferCommandHandler _handler;

    public TransferCommandHandlerTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<TransferCommandHandler>>();

        _handler = new TransferCommandHandler(
            _accountRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldTransferAmount_WhenAccountsExistAndAmountIsValid()
    {
        var source = CreateActiveAccountWithBalance(500);
        var destination = CreateActiveAccount();
        var metadata = new Dictionary<string, object> { { "source", "test" } };
        var command = new TransferCommand(
            source.Id,
            destination.Id,
            200,
            "REF-TRANSFER-001",
            "BRL",
            metadata);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(destination.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(destination);

        var result = await _handler.Handle(command, CancellationToken.None);

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        source.AvailableBalance.Should().Be(300);
        destination.AvailableBalance.Should().Be(200);

        result.Should().NotBeNull();
        result.Status.Should().Be(OperationStatus.Success);
        result.AvailableBalance.Should().Be(300);
        result.Balance.Should().Be(300);
        result.ErrorMessage.Should().BeNull();

        VerifyLoggerInformation("Processing transfer operation", Times.Once());
        VerifyLoggerInformation("Transfer operation completed successfully", Times.Once());
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentException_WhenSourceAndDestinationAreSame()
    {
        var accountId = Guid.NewGuid();
        var command = new TransferCommand(accountId, accountId, 100, "REF-123", "BRL", null);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*Source and destination accounts must be different.*");

        _accountRepositoryMock.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        VerifyLoggerInformation("Processing transfer operation", Times.Once());
        VerifyLoggerInformation("Transfer operation completed successfully", Times.Never());
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenSourceAccountDoesNotExist()
    {
        var sourceId = Guid.NewGuid();
        var destinationId = Guid.NewGuid();
        var command = new TransferCommand(sourceId, destinationId, 100, "REF-123", "BRL", null);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(sourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account)null!);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage($"*{nameof(Account)}*{sourceId}*");

        _accountRepositoryMock.Verify(repo => repo.GetByIdAsync(destinationId, It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        VerifyLoggerInformation("Processing transfer operation", Times.Once());
        VerifyLoggerInformation("Transfer operation completed successfully", Times.Never());
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenDestinationAccountDoesNotExist()
    {
        var source = CreateActiveAccount();
        var destinationId = Guid.NewGuid();
        var command = new TransferCommand(source.Id, destinationId, 100, "REF-123", "BRL", null);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(destinationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account)null!);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage($"*{nameof(Account)}*{destinationId}*");

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        VerifyLoggerInformation("Processing transfer operation", Times.Once());
        VerifyLoggerInformation("Transfer operation completed successfully", Times.Never());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailedOperation_WhenSourceHasInsufficientFunds()
    {
        var source = CreateActiveAccountWithBalance(50);
        var destination = CreateActiveAccount();
        var command = new TransferCommand(
            source.Id,
            destination.Id,
            100,
            "REF-TRANSFER-002",
            "BRL",
            null);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(destination.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(destination);

        var result = await _handler.Handle(command, CancellationToken.None);

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        result.Should().NotBeNull();
        result.Status.Should().Be(OperationStatus.Failed);
        result.ErrorMessage.Should().Contain("insufficient funds");

        source.AvailableBalance.Should().Be(50);
        destination.AvailableBalance.Should().Be(100);

        VerifyLoggerInformation("Processing transfer operation", Times.Once());
        VerifyLoggerInformation("Transfer operation completed successfully", Times.Once());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailedOperation_WhenSourceAccountIsInactive()
    {
        var source = CreateInactiveAccount();
        var destination = CreateActiveAccount();
        var command = new TransferCommand(
            source.Id,
            destination.Id,
            100,
            "REF-TRANSFER-003",
            "BRL",
            null);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(destination.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(destination);

        var result = await _handler.Handle(command, CancellationToken.None);

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        result.Should().NotBeNull();
        result.Status.Should().Be(OperationStatus.Failed);
        result.ErrorMessage.Should().ContainEquivalentOf("inactive");

        source.AvailableBalance.Should().Be(0);
        destination.AvailableBalance.Should().Be(100);

        VerifyLoggerInformation("Processing transfer operation", Times.Once());
        VerifyLoggerInformation("Transfer operation completed successfully", Times.Once());
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenSaveChangesFails()
    {
        var source = CreateActiveAccountWithBalance(500);
        var destination = CreateActiveAccount();
        var command = new TransferCommand(
            source.Id,
            destination.Id,
            200,
            "REF-TRANSFER-004",
            "BRL",
            null);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(destination.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(destination);

        _unitOfWorkMock
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Database error");

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        source.AvailableBalance.Should().Be(300);
        destination.AvailableBalance.Should().Be(200);

        VerifyLoggerInformation("Processing transfer operation", Times.Once());
        VerifyLoggerInformation("Transfer operation completed successfully", Times.Never());
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
