using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PagueVeloz.Application.Commands.Customers;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Tests.Commands.Customers;

public class CreateCustomerCommandHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<CreateCustomerCommandHandler>> _loggerMock;
    private readonly CreateCustomerCommandHandler _handler;

    public CreateCustomerCommandHandlerTests()
    {
        _customerRepositoryMock = new Mock<ICustomerRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<CreateCustomerCommandHandler>>();

        _handler = new CreateCustomerCommandHandler(
            _customerRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldCreateCustomer_WhenDataIsValid()
    {
        var command = new CreateCustomerCommand("John Doe", "12345678901");

        var result = await _handler.Handle(command, CancellationToken.None);

        _customerRepositoryMock.Verify(
            repo => repo.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        result.Should().NotBeNull();
        result.Name.Should().Be("John Doe");
        result.Document.Should().Be("12345678901");

        VerifyLoggerInformation("Creating customer", Times.Once());
        VerifyLoggerInformation("Customer created successfully", Times.Once());
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentException_WhenNameIsInvalid()
    {
        var command = new CreateCustomerCommand("", "12345678901");

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Customer name is required.*");

        _customerRepositoryMock.Verify(
            repo => repo.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        VerifyLoggerInformation("Creating customer", Times.Once());
        VerifyLoggerInformation("Customer created successfully", Times.Never());
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentException_WhenDocumentIsInvalid()
    {
        var command = new CreateCustomerCommand("John Doe", "123");

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Customer document must be a valid CPF or CNPJ.*");

        _customerRepositoryMock.Verify(
            repo => repo.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        VerifyLoggerInformation("Creating customer", Times.Once());
        VerifyLoggerInformation("Customer created successfully", Times.Never());
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenSaveChangesFails()
    {
        var command = new CreateCustomerCommand("John Doe", "12345678901");

        _unitOfWorkMock
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Database error");

        _customerRepositoryMock.Verify(
            repo => repo.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        VerifyLoggerInformation("Creating customer", Times.Once());
        VerifyLoggerInformation("Customer created successfully", Times.Never());
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
