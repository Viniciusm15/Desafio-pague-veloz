using MediatR;
using PagueVeloz.Application.DTOs.Transactions.Responses;

namespace PagueVeloz.Application.Commands.Transactions;

public record DebitCommand(
    Guid AccountId,
    long Amount,
    string ReferenceId,
    string Currency,
    Dictionary<string, object>? Metadata
) : IRequest<TransactionResponse>;
