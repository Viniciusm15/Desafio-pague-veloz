using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PagueVeloz.Application.Commands.Accounts;
using PagueVeloz.Application.Exceptions;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Enums;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Tests.Commands.Accounts;

public class ReactivateAccountCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<ReactivateAccountCommandHandler>> _loggerMock;
    private readonly ReactivateAccountCommandHandler _handler;

    public ReactivateAccountCommandHandlerTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<ReactivateAccountCommandHandler>>();

        _handler = new ReactivateAccountCommandHandler(
            _accountRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldReactivateAccount_WhenAccountExists()
    {
        var account = CreateInactiveAccount();
        var command = new ReactivateAccountCommand(account.Id);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var result = await _handler.Handle(command, CancellationToken.None);

        account.Status.Should().Be(AccountStatus.Active);

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        result.Should().NotBeNull();
        result.Id.Should().Be(account.Id);
        result.Status.Should().Be(AccountStatus.Active);

        VerifyLoggerInformation("Reactivating account", Times.Once());
        VerifyLoggerInformation($"Account reactivated successfully. AccountId {account.Id}, NewStatus {AccountStatus.Active}", Times.Once());
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenAccountDoesNotExist()
    {
        var accountId = Guid.NewGuid();
        var command = new ReactivateAccountCommand(accountId);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account)null!);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage($"*{nameof(Account)}*{accountId}*");

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        VerifyLoggerInformation("Reactivating account", Times.Once());
        VerifyLoggerInformation($"Account reactivated successfully. AccountId {accountId}, NewStatus {AccountStatus.Active}", Times.Never());
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenSaveChangesFails()
    {
        var account = CreateInactiveAccount();
        var command = new ReactivateAccountCommand(account.Id);

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

        account.Status.Should().Be(AccountStatus.Active);

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        VerifyLoggerInformation("Reactivating account", Times.Once());
        VerifyLoggerInformation($"Account reactivated successfully. AccountId {account.Id}, NewStatus {AccountStatus.Active}", Times.Never());
    }

    private static Account CreateInactiveAccount()
    {
        var customerId = Guid.NewGuid();
        var account = Account.Open(customerId, creditLimit: 0);
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
