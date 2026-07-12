using MediatR;
using PagueVeloz.Application.DTOs.Accounts.Responses;

namespace PagueVeloz.Application.Queries.Accounts;

public record GetAccountQuery(Guid AccountId) : IRequest<AccountResponse?>;
