using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PagueVeloz.Application.Commands.Transactions;
using PagueVeloz.Application.Exceptions;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Enums;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Tests.Commands.Transactions;

public class CreditCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<CreditCommandHandler>> _loggerMock;
    private readonly CreditCommandHandler _handler;

    public CreditCommandHandlerTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<CreditCommandHandler>>();

        _handler = new CreditCommandHandler(
            _accountRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldCreditAmount_WhenAccountExistsAndAmountIsValid()
    {
        var account = CreateActiveAccount();
        var metadata = new Dictionary<string, object> { { "source", "test" } };
        var command = new CreditCommand(
            account.Id,
            500,
            "REF-CREDIT-001",
            "BRL",
            metadata);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var result = await _handler.Handle(command, CancellationToken.None);

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        result.Should().NotBeNull();
        result.Status.Should().Be(OperationStatus.Success);
        result.Balance.Should().Be(500);
        result.ReservedBalance.Should().Be(0);
        result.AvailableBalance.Should().Be(500);
        result.ErrorMessage.Should().BeNull();

        VerifyLoggerInformation("Processing credit operation", Times.Once());
        VerifyLoggerInformation("Credit operation completed successfully", Times.Once());
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenAccountDoesNotExist()
    {
        var accountId = Guid.NewGuid();
        var command = new CreditCommand(accountId, 100, "REF-123", "BRL", null);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account)null!);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage($"*{nameof(Account)}*{accountId}*");

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        VerifyLoggerInformation("Processing credit operation", Times.Once());
        VerifyLoggerInformation("Credit operation completed successfully", Times.Never());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailedOperation_WhenCreditFailsDueToDomainValidation()
    {
        var account = CreateInactiveAccount();
        var command = new CreditCommand(
            account.Id,
            500,
            "REF-CREDIT-002",
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

        VerifyLoggerInformation("Processing credit operation", Times.Once());
        VerifyLoggerInformation("Credit operation completed successfully", Times.Once());
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenSaveChangesFails()
    {
        var account = CreateActiveAccount();
        var command = new CreditCommand(
            account.Id,
            200,
            "REF-CREDIT-003",
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

        VerifyLoggerInformation("Processing credit operation", Times.Once());
        VerifyLoggerInformation("Credit operation completed successfully", Times.Never());
    }

    private static Account CreateActiveAccount()
    {
        var customerId = Guid.NewGuid();
        return Account.Open(customerId, creditLimit: 0);
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
