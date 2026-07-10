using MediatR;
using PagueVeloz.Domain.Entities;

namespace PagueVeloz.Application.Queries.Accounts;

public record GetAccountQuery(Guid AccountId) : IRequest<Account?>;
