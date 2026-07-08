namespace PagueVeloz.Application.DTOs.Requests.Account;

public record ReserveAccountRequest(
    decimal Amount,
    string ReferenceId,
    string Currency = "BRL",
    Dictionary<string, object>? Metadata = null
);
