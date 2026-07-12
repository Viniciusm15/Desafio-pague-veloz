using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PagueVeloz.Application.Queries.Customers;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Tests.Queries.Customers;

public class GetCustomerQueryHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly Mock<ILogger<GetCustomerQueryHandler>> _loggerMock;
    private readonly GetCustomerQueryHandler _handler;

    public GetCustomerQueryHandlerTests()
    {
        _customerRepositoryMock = new Mock<ICustomerRepository>();
        _loggerMock = new Mock<ILogger<GetCustomerQueryHandler>>();
        _handler = new GetCustomerQueryHandler(
            _customerRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnCustomerResponse_WhenCustomerExists()
    {
        var customerId = Guid.NewGuid();
        var customer = CreateCustomer();
        var query = new GetCustomerQuery(customerId);

        _customerRepositoryMock
            .Setup(repo => repo.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(customer.Id);
        result.Name.Should().Be(customer.Name);
        result.Document.Should().Be(customer.Document);

        _customerRepositoryMock.Verify(
            repo => repo.GetByIdAsync(customerId, It.IsAny<CancellationToken>()),
            Times.Once);

        VerifyLoggerInformation("Getting customer", Times.Once());
        VerifyLoggerInformation("Customer retrieved successfully", Times.Once());
        VerifyLoggerWarning("Customer not found", Times.Never());
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenCustomerDoesNotExist()
    {
        var customerId = Guid.NewGuid();
        var query = new GetCustomerQuery(customerId);

        _customerRepositoryMock
            .Setup(repo => repo.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer)null!);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().BeNull();

        _customerRepositoryMock.Verify(
            repo => repo.GetByIdAsync(customerId, It.IsAny<CancellationToken>()),
            Times.Once);

        VerifyLoggerInformation("Getting customer", Times.Once());
        VerifyLoggerWarning("Customer not found", Times.Once());
        VerifyLoggerInformation("Customer retrieved successfully", Times.Never());
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
