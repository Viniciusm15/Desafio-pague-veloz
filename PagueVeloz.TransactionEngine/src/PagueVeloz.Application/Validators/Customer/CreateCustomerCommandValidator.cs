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

        RuleFor(x => x.Document)
            .IsValidCPF()
            .When(x => x.Document.Replace(".", "").Replace("-", "").Replace("/", "").Length <= 11)
            .WithMessage("document must be a valid CPF.");

        RuleFor(x => x.Document)
            .IsValidCNPJ()
            .When(x => x.Document.Replace(".", "").Replace("-", "").Replace("/", "").Length > 11)
            .WithMessage("document must be a valid CNPJ.");
    }
}
