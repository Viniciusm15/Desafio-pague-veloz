using MediatR;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Queries.Customers;

public class GetCustomerQueryHandler : IRequestHandler<GetCustomerQuery, Customer?>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Customer?> Handle(GetCustomerQuery request, CancellationToken cancellationToken)
    {
        return await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
    }
}
