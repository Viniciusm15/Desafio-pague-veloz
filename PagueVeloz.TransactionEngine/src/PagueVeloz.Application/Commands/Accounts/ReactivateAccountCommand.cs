using MediatR;
using PagueVeloz.Application.DTOs.Accounts.Responses;

namespace PagueVeloz.Application.Commands.Accounts;

public record ReactivateAccountCommand(Guid AccountId) : IRequest<AccountResponse>;
