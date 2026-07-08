namespace PagueVeloz.API.Models;

public record CreateAccountRequest(Guid CustomerId, decimal CreditLimit = 0m);
