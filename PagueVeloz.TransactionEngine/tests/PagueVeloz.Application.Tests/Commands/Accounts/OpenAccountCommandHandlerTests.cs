using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PagueVeloz.Application.Commands.Accounts;
using PagueVeloz.Application.Exceptions;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Enums;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Tests.Commands.Accounts;

public class OpenAccountCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _accountRepositoryMock;
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<OpenAccountCommandHandler>> _loggerMock;
    private readonly OpenAccountCommandHandler _handler;

    public OpenAccountCommandHandlerTests()
    {
        _accountRepositoryMock = new Mock<IAccountRepository>();
        _customerRepositoryMock = new Mock<ICustomerRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<OpenAccountCommandHandler>>();

        _handler = new OpenAccountCommandHandler(
            _accountRepositoryMock.Object,
            _customerRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldOpenAccount_WhenCustomerExists()
    {
        var customer = CreateCustomer();
        var creditLimit = 1000L;
        var command = new OpenAccountCommand(customer.Id, creditLimit);

        _customerRepositoryMock
            .Setup(repo => repo.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await _handler.Handle(command, CancellationToken.None);

        _accountRepositoryMock.Verify(
            repo => repo.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        result.Should().NotBeNull();
        result.CustomerId.Should().Be(customer.Id);
        result.CreditLimit.Should().Be(creditLimit);
        result.Status.Should().Be(AccountStatus.Active);

        VerifyLoggerInformation("Opening account", Times.Once());
        VerifyLoggerInformation($"Account opened successfully", Times.Once());
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenCustomerDoesNotExist()
    {
        var customerId = Guid.NewGuid();
        var command = new OpenAccountCommand(customerId, 0);

        _customerRepositoryMock
            .Setup(repo => repo.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer)null!);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage($"*{nameof(Customer)}*{customerId}*");

        _accountRepositoryMock.Verify(
            repo => repo.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        VerifyLoggerInformation("Opening account", Times.Once());
        VerifyLoggerInformation($"Account opened successfully", Times.Never());
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenSaveChangesFails()
    {
        var customer = CreateCustomer();
        var creditLimit = 1000L;
        var command = new OpenAccountCommand(customer.Id, creditLimit);

        _customerRepositoryMock
            .Setup(repo => repo.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _unitOfWorkMock
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Database error");

        _accountRepositoryMock.Verify(
            repo => repo.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        VerifyLoggerInformation("Opening account", Times.Once());
        VerifyLoggerInformation($"Account opened successfully", Times.Never());
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentException_WhenCreditLimitIsNegative()
    {
        var customer = CreateCustomer();
        var creditLimit = -100L;
        var command = new OpenAccountCommand(customer.Id, creditLimit);

        _customerRepositoryMock
            .Setup(repo => repo.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("*Credit limit cannot be negative.*");

        _accountRepositoryMock.Verify(
            repo => repo.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        VerifyLoggerInformation("Opening account", Times.Once());
        VerifyLoggerInformation($"Account opened successfully", Times.Never());
    }

    private static Customer CreateCustomer()
    {
        return Customer.Create("John Doe", "12345678901");
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
