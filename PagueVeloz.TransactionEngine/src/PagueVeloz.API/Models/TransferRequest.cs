namespace PagueVeloz.API.Models;

public record TransferRequest(Guid SourceAccountId, Guid DestinationAccountId, decimal Amount);
