namespace PagueVeloz.Application.DTOs.Requests;

public record CreateAccountRequest(Guid CustomerId, decimal CreditLimit = 0m);
