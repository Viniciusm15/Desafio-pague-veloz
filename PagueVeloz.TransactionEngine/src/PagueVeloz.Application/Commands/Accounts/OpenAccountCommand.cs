using MediatR;
using PagueVeloz.Application.DTOs.Accounts.Responses;

namespace PagueVeloz.Application.Commands.Accounts;

public record OpenAccountCommand(Guid CustomerId, decimal CreditLimit = 0m) : IRequest<AccountResponse>;
