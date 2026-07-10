using MediatR;
using PagueVeloz.Domain.Entities;

namespace PagueVeloz.Application.Commands.Accounts;

public record DeactivateAccountCommand(Guid AccountId) : IRequest<Account>;
