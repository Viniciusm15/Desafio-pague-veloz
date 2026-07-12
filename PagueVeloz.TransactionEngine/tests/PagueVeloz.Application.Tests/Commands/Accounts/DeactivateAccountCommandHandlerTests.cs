using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PagueVeloz.Application.Commands.Accounts;
using PagueVeloz.Application.Exceptions;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Enums;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Tests.Commands.Accounts;

public class DeactivateAccountCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<DeactivateAccountCommandHandler>> _loggerMock;
    private readonly DeactivateAccountCommandHandler _handler;

    public DeactivateAccountCommandHandlerTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<DeactivateAccountCommandHandler>>();

        _handler = new DeactivateAccountCommandHandler(
            _accountRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldDeactivateAccount_WhenAccountExists()
    {
        var accountId = Guid.NewGuid();
        var command = new DeactivateAccountCommand(accountId);
        var account = CreateActiveAccount();

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var result = await _handler.Handle(command, CancellationToken.None);

        account.Status.Should().Be(AccountStatus.Inactive);

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        result.Should().NotBeNull();
        result.Id.Should().Be(account.Id);
        result.Status.Should().Be(AccountStatus.Inactive);

        VerifyLoggerInformation("Deactivating account", Times.Once());
        VerifyLoggerInformation($"Account deactivated successfully. AccountId {account.Id}, NewStatus {AccountStatus.Inactive}", Times.Once());
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenAccountDoesNotExist()
    {
        var accountId = Guid.NewGuid();
        var command = new DeactivateAccountCommand(accountId);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account)null!);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage($"*{nameof(Account)}*{accountId}*");

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        VerifyLoggerInformation("Deactivating account", Times.Once());
        VerifyLoggerInformation($"Account deactivated successfully. AccountId {accountId}, NewStatus {AccountStatus.Inactive}", Times.Never());
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenSaveChangesFails()
    {
        var accountId = Guid.NewGuid();
        var command = new DeactivateAccountCommand(accountId);
        var account = CreateActiveAccount();

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _unitOfWorkMock
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Database error");

        account.Status.Should().Be(AccountStatus.Inactive);

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        VerifyLoggerInformation("Deactivating account", Times.Once());
        VerifyLoggerInformation($"Account deactivated successfully. AccountId {account.Id}, NewStatus {AccountStatus.Inactive}", Times.Never());
    }

    private static Account CreateActiveAccount()
    {
        var customerId = Guid.NewGuid();
        return Account.Open(customerId, creditLimit: 0);
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
