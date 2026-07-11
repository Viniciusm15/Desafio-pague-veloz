using PagueVeloz.Domain.Common;

namespace PagueVeloz.Domain.Events;

public record AccountCreditedEvent(
    Guid AccountId,
    Guid OperationId,
    decimal Amount,
    string Currency,
    string ReferenceId
) : DomainEventBase;
