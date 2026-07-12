using Microsoft.EntityFrameworkCore;
using PagueVeloz.Infrastructure.Persistence.Context;
using Serilog.Context;

namespace PagueVeloz.Worker;

public class OutboxProcessorWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorWorker> _logger;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    public OutboxProcessorWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessorWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while processing the outbox.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }

        _logger.LogInformation("Outbox processor worker stopped.");
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;
        var pending = await context.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.NextAttemptAt <= now)
            .OrderBy(m => m.OccurredOn)
            .Take(20)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
            return;

        _logger.LogInformation(
            "Found {PendingMessages} pending outbox message(s) to process.",
            pending.Count);

        foreach (var message in pending)
        {
            var correlationId = message.CorrelationId;

            try
            {
                using (LogContext.PushProperty("CorrelationId", correlationId))
                {
                    _logger.LogInformation(
                        "Publishing event {EventType} ({MessageId}).",
                        message.EventType,
                        message.Id);

                    message.MarkProcessed();

                    _logger.LogInformation(
                        "Successfully processed event {EventType} ({MessageId}).",
                        message.EventType,
                        message.Id);
                }
            }
            catch (Exception ex)
            {
                message.RegisterFailedAttempt();

                using (LogContext.PushProperty("CorrelationId", correlationId))
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to process event {EventType} ({MessageId}). Attempt {RetryCount}. Next retry at {NextAttemptAt}.",
                        message.EventType,
                        message.Id,
                        message.Attempts,
                        message.NextAttemptAt);
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "{ProcessedMessages} outbox message(s) processed successfully.",
            pending.Count);
    }
}
