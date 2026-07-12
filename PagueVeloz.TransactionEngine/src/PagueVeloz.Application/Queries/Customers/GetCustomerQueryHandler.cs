using MediatR;
using Microsoft.Extensions.Logging;
using PagueVeloz.Application.DTOs.Customers.Responses;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Queries.Customers;

public class GetCustomerQueryHandler : IRequestHandler<GetCustomerQuery, CustomerResponse?>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ILogger<GetCustomerQueryHandler> _logger;

    public GetCustomerQueryHandler(
        ICustomerRepository customerRepository,
        ILogger<GetCustomerQueryHandler> logger)
    {
        _customerRepository = customerRepository;
        _logger = logger;
    }

    public async Task<CustomerResponse?> Handle(GetCustomerQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting customer. CustomerId {CustomerId}", request.CustomerId);

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);

        if (customer is null)
        {
            _logger.LogWarning("Customer not found. CustomerId {CustomerId}", request.CustomerId);
            return null;
        }

        _logger.LogInformation(
            "Customer retrieved successfully. CustomerId {CustomerId}, Name {Name}, Document {Document}",
            customer.Id,
            customer.Name,
            customer.Document);

        return CustomerResponse.From(customer);
    }
}
