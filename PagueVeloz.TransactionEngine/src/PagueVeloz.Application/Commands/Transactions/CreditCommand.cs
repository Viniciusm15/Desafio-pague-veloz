using MediatR;
using PagueVeloz.Application.DTOs.Responses;

namespace PagueVeloz.Application.Commands.Transactions;

public record CreditCommand(
    Guid AccountId,
    decimal Amount,
    string ReferenceId,
    string Currency,
    Dictionary<string, object>? Metadata
) : IRequest<TransactionResponse>;
