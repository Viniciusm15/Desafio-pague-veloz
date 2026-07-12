using MediatR;
using PagueVeloz.Application.DTOs.Transactions.Responses;

namespace PagueVeloz.Application.Commands.Transactions;

public record CaptureCommand(
    Guid AccountId,
    Guid ReserveOperationId,
    string ReferenceId,
    string Currency,
    Dictionary<string, object>? Metadata
) : IRequest<TransactionResponse>;
