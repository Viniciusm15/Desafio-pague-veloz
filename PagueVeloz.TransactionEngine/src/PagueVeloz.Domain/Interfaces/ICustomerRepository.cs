using PagueVeloz.Domain.Entities;

namespace PagueVeloz.Domain.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid customerId);
    Task AddAsync(Customer customer);
}
