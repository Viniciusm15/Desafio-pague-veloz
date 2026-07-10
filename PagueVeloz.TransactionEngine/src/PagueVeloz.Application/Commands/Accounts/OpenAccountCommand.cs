using MediatR;
using PagueVeloz.Domain.Entities;

namespace PagueVeloz.Application.Commands.Accounts;

public record OpenAccountCommand(Guid CustomerId, decimal CreditLimit = 0m) : IRequest<Account>;
