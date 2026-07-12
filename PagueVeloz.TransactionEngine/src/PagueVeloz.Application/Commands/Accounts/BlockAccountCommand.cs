using MediatR;
using PagueVeloz.Application.DTOs.Accounts.Responses;

namespace PagueVeloz.Application.Commands.Accounts;

public record BlockAccountCommand(Guid AccountId) : IRequest<AccountResponse>;
