namespace PagueVeloz.Application.DTOs.Requests;

public record TransferRequest(Guid SourceAccountId, Guid DestinationAccountId, decimal Amount, string ReferenceId);
