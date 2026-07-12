using MediatR;
using PagueVeloz.Application.DTOs.Accounts.Responses;

namespace PagueVeloz.Application.Commands.Accounts;

public record OpenAccountCommand(Guid CustomerId, long CreditLimit = 0) : IRequest<AccountResponse>;
