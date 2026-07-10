using MediatR;
using PagueVeloz.Application.DTOs.Customers.Responses;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Queries.Customers;

public class GetCustomerQueryHandler : IRequestHandler<GetCustomerQuery, CustomerResponse?>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CustomerResponse?> Handle(GetCustomerQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        return customer is null ? null : CustomerResponse.From(customer);
    }
}
