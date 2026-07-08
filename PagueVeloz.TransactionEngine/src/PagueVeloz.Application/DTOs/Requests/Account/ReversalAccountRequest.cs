namespace PagueVeloz.Application.DTOs.Requests.Account;

public record ReversalAccountRequest(
    Guid OriginalOperationId,
    string ReferenceId,
    string Currency = "BRL",
    Dictionary<string, object>? Metadata = null
);
