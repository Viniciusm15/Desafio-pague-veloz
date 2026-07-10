using FluentValidation;
using PagueVeloz.Application.Commands.Customers;

namespace PagueVeloz.Application.Validators.Customer;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("name is required.");

        RuleFor(x => x.Document)
            .NotEmpty()
            .WithMessage("document is required.");
    }
}
