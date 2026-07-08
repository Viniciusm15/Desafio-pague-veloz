using PagueVeloz.Application.DTOs.Requests;
using PagueVeloz.Domain.Entities;

namespace PagueVeloz.Application.Interfaces;

public interface ICustomerService
{
    Task<Customer> CreateAsync(CreateCustomerRequest request);
    Task<Customer?> GetByIdAsync(Guid customerId);
}
