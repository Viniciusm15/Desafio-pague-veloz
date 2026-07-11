using PagueVeloz.Domain.Common;

namespace PagueVeloz.Domain.Events;

public record AccountDebitedEvent(
    Guid AccountId,
    Guid OperationId,
    decimal Amount,
    string Currency,
    string ReferenceId
) : DomainEventBase;
