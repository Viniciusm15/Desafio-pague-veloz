namespace PagueVeloz.Application.DTOs.Requests.Account;

public record CaptureAccountRequest(
    Guid ReserveOperationId,
    string ReferenceId,
    string Currency = "BRL",
    Dictionary<string, object>? Metadata = null
);
