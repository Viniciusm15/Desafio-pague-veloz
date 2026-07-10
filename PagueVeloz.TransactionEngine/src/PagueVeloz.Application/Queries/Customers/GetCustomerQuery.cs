using MediatR;
using PagueVeloz.Domain.Entities;

namespace PagueVeloz.Application.Queries.Customers;

public record GetCustomerQuery(Guid CustomerId) : IRequest<Customer?>;
