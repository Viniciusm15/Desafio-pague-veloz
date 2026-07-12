namespace PagueVeloz.Infrastructure.Observability;

public interface ICorrelationIdProvider
{
    string? CorrelationId { get; }
    void SetCorrelationId(string correlationId);
}
