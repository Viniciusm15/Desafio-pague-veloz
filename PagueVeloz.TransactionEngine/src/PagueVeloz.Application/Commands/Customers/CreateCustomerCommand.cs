using MediatR;
using PagueVeloz.Domain.Entities;

namespace PagueVeloz.Application.Commands.Customers;

public record CreateCustomerCommand(string Name, string Document) : IRequest<Customer>;
