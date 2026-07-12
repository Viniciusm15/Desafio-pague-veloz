using FluentValidation;
using PagueVeloz.Application.Commands.Accounts;

namespace PagueVeloz.Application.Validators.Account;

public class OpenAccountCommandValidator : AbstractValidator<OpenAccountCommand>
{
    public OpenAccountCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("customer_id is required.");

        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0)
            .WithMessage("credit_limit cannot be negative.");
    }
}
