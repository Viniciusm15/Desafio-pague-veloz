namespace PagueVeloz.API.Models;

public record ReversalAccountRequest(Guid OriginalOperationId, string ReferenceId);
