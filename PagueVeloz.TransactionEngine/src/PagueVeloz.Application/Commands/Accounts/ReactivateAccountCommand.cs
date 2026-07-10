using MediatR;
using PagueVeloz.Domain.Entities;

namespace PagueVeloz.Application.Commands.Accounts;

public record ReactivateAccountCommand(Guid AccountId) : IRequest<Account>;
