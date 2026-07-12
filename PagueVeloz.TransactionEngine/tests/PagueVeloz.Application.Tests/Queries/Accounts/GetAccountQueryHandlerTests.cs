using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PagueVeloz.Application.Queries.Accounts;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Tests.Queries.Accounts;

public class GetAccountQueryHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<ILogger<GetAccountQueryHandler>> _loggerMock;
    private readonly GetAccountQueryHandler _handler;

    public GetAccountQueryHandlerTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _loggerMock = new Mock<ILogger<GetAccountQueryHandler>>();
        _handler = new GetAccountQueryHandler(
            _accountRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnAccountResponse_WhenAccountExists()
    {
        var accountId = Guid.NewGuid();
        var account = CreateActiveAccount();
        var query = new GetAccountQuery(accountId);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(account.Id);
        result.Status.Should().Be(account.Status);

        _accountRepositoryMock.Verify(
            repo => repo.GetByIdAsync(accountId, It.IsAny<CancellationToken>()),
            Times.Once);

        VerifyLoggerInformation("Getting account", Times.Once());
        VerifyLoggerInformation("Account retrieved successfully", Times.Once());
        VerifyLoggerWarning("Account not found", Times.Never());
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenAccountDoesNotExist()
    {
        var accountId = Guid.NewGuid();
        var query = new GetAccountQuery(accountId);

        _accountRepositoryMock
            .Setup(repo => repo.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account)null!);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().BeNull();

        _accountRepositoryMock.Verify(
            repo => repo.GetByIdAsync(accountId, It.IsAny<CancellationToken>()),
            Times.Once);

        VerifyLoggerInformation("Getting account", Times.Once());
        VerifyLoggerWarning("Account not found", Times.Once());
        VerifyLoggerInformation("Account retrieved successfully", Times.Never());
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

    private void VerifyLoggerWarning(string expectedMessage, Times times)
    {
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            times);
    }
}
