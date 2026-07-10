using MediatR;
using PagueVeloz.Domain.Entities;

namespace PagueVeloz.Application.Commands.Accounts;

public record BlockAccountCommand(Guid AccountId) : IRequest<Account>;
