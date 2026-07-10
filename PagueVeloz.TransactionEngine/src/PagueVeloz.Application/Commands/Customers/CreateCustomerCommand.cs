using MediatR;
using PagueVeloz.Application.DTOs.Customers.Responses;

namespace PagueVeloz.Application.Commands.Customers;

public record CreateCustomerCommand(string Name, string Document) : IRequest<CustomerResponse>;
