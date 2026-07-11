namespace PagueVeloz.Infrastructure.Messaging.Outbox;

public class OutboxMessage
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime OccurredOn { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public int Attempts { get; private set; }
    public DateTime? NextAttemptAt { get; private set; }

    private OutboxMessage() { }

    public OutboxMessage(string eventType, string payload, DateTime occurredOn)
    {
        Id = Guid.NewGuid();
        EventType = eventType;
        Payload = payload;
        OccurredOn = occurredOn;
        Attempts = 0;
        NextAttemptAt = occurredOn;
    }

    public void MarkProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
        NextAttemptAt = null;
    }

    public void RegisterFailedAttempt()
    {
        Attempts++;
        NextAttemptAt = DateTime.UtcNow.AddSeconds(Math.Pow(2, Attempts));
    }
}
