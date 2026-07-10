using MediatR;
using PagueVeloz.Application.DTOs.Customers.Responses;

namespace PagueVeloz.Application.Queries.Customers;

public record GetCustomerQuery(Guid CustomerId) : IRequest<CustomerResponse?>;
