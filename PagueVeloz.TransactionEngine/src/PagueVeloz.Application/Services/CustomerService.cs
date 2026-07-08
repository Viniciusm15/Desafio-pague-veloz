using PagueVeloz.Application.DTOs.Requests;
using PagueVeloz.Application.Interfaces;
using PagueVeloz.Domain.Entities;
using PagueVeloz.Domain.Interfaces;

namespace PagueVeloz.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Customer> CreateAsync(CreateCustomerRequest request)
    {
        var customer = Customer.Create(request.Name, request.Document);
        await _customerRepository.AddAsync(customer);

        return customer;
    }

    public Task<Customer?> GetByIdAsync(Guid customerId)
    {
        return _customerRepository.GetByIdAsync(customerId);
    }
}
