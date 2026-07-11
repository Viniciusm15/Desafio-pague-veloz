using Microsoft.EntityFrameworkCore.Diagnostics;
using PagueVeloz.Domain.Common;
using PagueVeloz.Infrastructure.Messaging.Outbox;
using System.Text.Json;

namespace PagueVeloz.Infrastructure.Persistence.Interceptors;

public class DomainEventInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var outboxEntries = new List<OutboxMessage>();

        foreach (var entry in context.ChangeTracker.Entries<Entity>())
        {
            if (entry.Entity is not Entity entity)
                continue;

            var events = entity.DomainEvents.ToList();
            if (events.Count == 0)
                continue;

            foreach (var domainEvent in events)
            {
                if (domainEvent is IDomainEvent @event)
                {
                    outboxEntries.Add(new OutboxMessage(
                        eventType: @event.GetType().FullName!,
                        payload: JsonSerializer.Serialize(@event, new JsonSerializerOptions
                        {
                            WriteIndented = false
                        }),
                        occurredOn: @event.OccurredOn
                    ));
                }
            }

            entity.ClearDomainEvents();
        }

        if (outboxEntries.Count != 0)
        {
            context.Set<OutboxMessage>().AddRange(outboxEntries);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
