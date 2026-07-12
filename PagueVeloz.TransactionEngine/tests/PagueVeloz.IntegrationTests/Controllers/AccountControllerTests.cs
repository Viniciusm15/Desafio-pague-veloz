using FluentAssertions;
using PagueVeloz.Application.DTOs.Customers.Responses;
using PagueVeloz.Application.DTOs.Transactions.Responses;
using PagueVeloz.Domain.Enums;
using PagueVeloz.IntegrationTests.Builders;
using PagueVeloz.IntegrationTests.Common;
using System.Net;
using System.Text.Json;

namespace PagueVeloz.IntegrationTests.Controllers;

[Collection("Database")]
public class AccountControllerTests(IntegrationTestWebAppFactory factory) : IntegrationTestBase(factory)
{
    private async Task<Guid> CreateCustomerAsync()
    {
        var payload = new CustomerRequestBuilder().Build();
        var response = await PostAsJsonAsync("/api/customer", payload);
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create customer: {response.StatusCode} - {errorBody}");
        }
        var customer = await ReadFromJsonAsync<CustomerResponse>(response);
        customer.Should().NotBeNull();
        return customer!.Id;
    }

    private async Task<Guid> CreateAccountAsync(Guid customerId, long creditLimit = 0)
    {
        var payload = new { customer_id = customerId, credit_limit = creditLimit };
        var response = await PostAsJsonAsync("/api/account", payload);
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create account: {response.StatusCode} - {errorBody}");
        }

        using var jsonDoc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var idProperty = jsonDoc.RootElement.GetProperty("id");
        return idProperty.GetGuid();
    }

    [Fact]
    public async Task Execute_CreditOperation_WithValidPayload_ReturnsOkAndUpdatesBalance()
    {
        var customerId = await CreateCustomerAsync();
        var accountId = await CreateAccountAsync(customerId);

        var request = new TransactionRequestBuilder()
            .WithOperation(OperationType.Credit)
            .WithAccountId(accountId)
            .WithAmount(5000)
            .Build();

        var response = await PostAsJsonAsync("/api/account/transactions", request);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Erro: {error}");
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ReadFromJsonAsync<TransactionResponse>(response);
        result.Should().NotBeNull();
        result!.Status.Should().Be(OperationStatus.Success);
        result.AvailableBalance.Should().Be(5000);
    }

    [Fact]
    public async Task Execute_DebitOperation_WithInsufficientBalance_ReturnsFailedStatus()
    {
        var customerId = await CreateCustomerAsync();
        var accountId = await CreateAccountAsync(customerId);

        var request = new TransactionRequestBuilder()
            .WithOperation(OperationType.Debit)
            .WithAccountId(accountId)
            .WithAmount(1000)
            .Build();

        var response = await PostAsJsonAsync("/api/account/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ReadFromJsonAsync<TransactionResponse>(response);
        result.Should().NotBeNull();
        result!.Status.Should().Be(OperationStatus.Failed);
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Execute_CreditThenDebit_ReturnsCorrectFinalBalance()
    {
        var customerId = await CreateCustomerAsync();
        var accountId = await CreateAccountAsync(customerId);

        var creditRequest = new TransactionRequestBuilder()
            .WithOperation(OperationType.Credit)
            .WithAccountId(accountId)
            .WithAmount(10000)
            .Build();

        var debitRequest = new TransactionRequestBuilder()
            .WithOperation(OperationType.Debit)
            .WithAccountId(accountId)
            .WithAmount(3000)
            .Build();

        await PostAsJsonAsync("/api/account/transactions", creditRequest);
        var response = await PostAsJsonAsync("/api/account/transactions", debitRequest);

        var result = await ReadFromJsonAsync<TransactionResponse>(response);
        result.Should().NotBeNull();
        result!.Status.Should().Be(OperationStatus.Success);
        result.AvailableBalance.Should().Be(7000);
    }

    [Fact]
    public async Task Execute_CaptureOperation_WithValidReserve_ReturnsSuccess()
    {
        var customerId = await CreateCustomerAsync();
        var accountId = await CreateAccountAsync(customerId);

        var creditRequest = new TransactionRequestBuilder()
            .WithOperation(OperationType.Credit)
            .WithAccountId(accountId)
            .WithAmount(10000)
            .Build();
        await PostAsJsonAsync("/api/account/transactions", creditRequest);

        var reserveRequest = new TransactionRequestBuilder()
            .WithOperation(OperationType.Reserve)
            .WithAccountId(accountId)
            .WithAmount(3000)
            .Build();
        var reserveResponse = await PostAsJsonAsync("/api/account/transactions", reserveRequest);
        var reserveResult = await ReadFromJsonAsync<TransactionResponse>(reserveResponse);
        var reserveOperationId = reserveResult!.TransactionId;

        var captureRequest = new TransactionRequestBuilder()
            .WithOperation(OperationType.Capture)
            .WithAccountId(accountId)
            .WithReserveOperationId(reserveOperationId)
            .Build();

        var response = await PostAsJsonAsync("/api/account/transactions", captureRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ReadFromJsonAsync<TransactionResponse>(response);
        result.Should().NotBeNull();
        result!.Status.Should().Be(OperationStatus.Success);
        result.AvailableBalance.Should().Be(7000);
        result.ReservedBalance.Should().Be(0);
    }
}
