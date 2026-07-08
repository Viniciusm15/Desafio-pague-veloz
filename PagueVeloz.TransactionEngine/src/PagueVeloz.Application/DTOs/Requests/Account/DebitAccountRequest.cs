namespace PagueVeloz.Application.DTOs.Requests.Account;

public record DebitAccountRequest(
    decimal Amount,
    string ReferenceId,
    string Currency = "BRL",
    Dictionary<string, object>? Metadata = null
);
