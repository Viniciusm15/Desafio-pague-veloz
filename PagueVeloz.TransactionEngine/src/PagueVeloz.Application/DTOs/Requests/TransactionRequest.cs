using PagueVeloz.Domain.Enums;

namespace PagueVeloz.Application.DTOs.Requests;

public record TransactionRequest(
    OperationType Operation,
    Guid AccountId,
    decimal Amount,
    string ReferenceId,
    string Currency = "BRL",
    Dictionary<string, object>? Metadata = null,
    Guid? ReserveOperationId = null,
    Guid? OriginalOperationId = null,
    Guid? DestinationAccountId = null
);
