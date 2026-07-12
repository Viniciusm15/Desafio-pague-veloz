using FluentAssertions;
using PagueVeloz.Domain.Entities;

namespace PagueVeloz.Domain.Tests.Entities;

public class CustomerTests
{
    #region Create - Success

    [Fact]
    public void Create_WithValidNameAndCpf_ShouldCreateCustomerWithNormalizedDocument()
    {
        // Arrange
        const string name = "João Silva";
        const string document = "123.456.789-09";

        // Act
        var customer = Customer.Create(name, document);

        // Assert
        customer.Should().NotBeNull();
        customer.Id.Should().NotBeEmpty();
        customer.Name.Should().Be(name);
        customer.Document.Should().Be("12345678909");
        customer.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithValidNameAndCnpj_ShouldCreateCustomerWithNormalizedDocument()
    {
        // Arrange
        const string name = "Empresa LTDA";
        const string document = "12.345.678/0001-99";

        // Act
        var customer = Customer.Create(name, document);

        // Assert
        customer.Document.Should().Be("12345678000199");
    }

    [Fact]
    public void Create_WithDocumentAlreadyNormalized_ShouldKeepAsIs()
    {
        // Arrange
        const string name = "Maria Santos";
        const string document = "12345678909";

        // Act
        var customer = Customer.Create(name, document);

        // Assert
        customer.Document.Should().Be("12345678909");
    }

    [Fact]
    public void Create_WithCnpjAlreadyNormalized_ShouldKeepAsIs()
    {
        // Arrange
        const string name = "Tech Solutions";
        const string document = "12345678000199";

        // Act
        var customer = Customer.Create(name, document);

        // Assert
        customer.Document.Should().Be("12345678000199");
    }

    #endregion

    #region Create - Name Validation

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ShouldThrowArgumentException(string invalidName)
    {
        // Act
        Action act = () => Customer.Create(invalidName, "12345678909");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Customer name is required.*");
    }

    #endregion

    #region Create - Document Validation

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyDocument_ShouldThrowArgumentException(string invalidDocument)
    {
        // Act
        Action act = () => Customer.Create("João Silva", invalidDocument);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Customer document is required.*");
    }

    [Theory]
    [InlineData("1234567890")]
    [InlineData("123456789012")]
    [InlineData("123456789012345")]
    [InlineData("123.456.789-0")]
    public void Create_WithInvalidDocumentLength_ShouldThrowArgumentException(string invalidDocument)
    {
        // Act
        Action act = () => Customer.Create("João Silva", invalidDocument);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Customer document must be a valid CPF or CNPJ.*");
    }

    [Fact]
    public void Create_WithDocumentContainingNonDigitCharacters_ShouldThrowArgumentException()
    {
        // Arrange
        const string invalidDocument = "1234567890A";

        // Act
        Action act = () => Customer.Create("João Silva", invalidDocument);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Customer document must be a valid CPF or CNPJ.*");
    }

    #endregion

    #region Domain Events

    [Fact]
    public void Create_ShouldNotRaiseAnyDomainEvent()
    {
        // Arrange & Act
        var customer = Customer.Create("João Silva", "12345678909");

        // Assert
        customer.DomainEvents.Should().BeEmpty();
    }

    #endregion
}