using MediatR;
using PagueVeloz.Application.DTOs.Responses;

namespace PagueVeloz.Application.Commands.Transactions;

public record ReversalCommand(
    Guid AccountId,
    Guid OriginalOperationId,
    string ReferenceId,
    string Currency,
    Dictionary<string, object>? Metadata
) : IRequest<TransactionResponse>;
