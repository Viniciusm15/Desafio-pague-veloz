using PagueVeloz.Domain.Entities;

namespace PagueVeloz.Application.DTOs.Customers.Responses;

public record CustomerResponse(Guid Id, string Name, string Document)
{
    public static CustomerResponse From(Customer customer) => new(
        customer.Id,
        customer.Name,
        customer.Document
    );
}
