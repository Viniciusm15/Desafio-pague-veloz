using PagueVeloz.Domain.Common;

namespace PagueVeloz.Domain.Events;

public record FundsCapturedEvent(
    Guid AccountId,
    Guid OperationId,
    Guid ReservationOperationId,
    decimal Amount,
    string Currency,
    string ReferenceId
) : DomainEventBase;
