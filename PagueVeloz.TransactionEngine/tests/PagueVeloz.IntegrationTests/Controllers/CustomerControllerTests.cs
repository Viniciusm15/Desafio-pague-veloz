using FluentAssertions;
using PagueVeloz.Application.DTOs.Customers.Responses;
using PagueVeloz.IntegrationTests.Builders;
using PagueVeloz.IntegrationTests.Common;
using System.Net;
using System.Net.Http.Json;

namespace PagueVeloz.IntegrationTests.Controllers;

[Collection("Database")]
public class CustomerControllerTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task CreateCustomer_WithValidData_ReturnsCreatedAndCustomerId()
    {
        // Arrange
        var builder = new CustomerRequestBuilder()
            .WithName("Jane Smith")
            .WithDocument("52998224725");
        var payload = builder.Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/customer", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        customer.Should().NotBeNull();
        customer!.Id.Should().NotBeEmpty();
        customer.Name.Should().Be("Jane Smith");
        customer.Document.Should().Be("52998224725");
    }

    [Fact]
    public async Task CreateCustomer_WithInvalidDocument_ReturnsBadRequest()
    {
        // Arrange
        var payload = new CustomerRequestBuilder()
            .WithDocument("123")
            .Build();

        // Act
        var response = await Client.PostAsJsonAsync("/api/customer", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadAsStringAsync();
        error.Should().Contain("document");
    }

    [Fact]
    public async Task GetCustomerById_WhenExists_ReturnsOkWithCustomer()
    {
        // Arrange
        var createResponse = await Client.PostAsJsonAsync("/api/customer", new CustomerRequestBuilder().Build());
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerResponse>();
        var customerId = created!.Id;

        // Act
        var response = await Client.GetAsync($"/api/customer/{customerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        customer.Should().NotBeNull();
        customer!.Id.Should().Be(customerId);
        customer.Name.Should().Be("John Doe");
        customer.Document.Should().Be("52998224725");
    }

    [Fact]
    public async Task GetCustomerById_WhenNotExists_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/customer/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
