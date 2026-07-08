namespace PagueVeloz.Application.DTOs.Requests;

public record ReversalAccountRequest(Guid OriginalOperationId, string ReferenceId);
