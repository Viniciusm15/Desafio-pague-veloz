using FluentValidation;
using PagueVeloz.Application.Commands.Transactions;

namespace PagueVeloz.Application.Validators.Account;

public class TransferCommandValidator : AbstractValidator<TransferCommand>
{
    public TransferCommandValidator()
    {
        RuleFor(x => x.SourceAccountId)
            .NotEmpty()
            .WithMessage("source_account_id is required.");

        RuleFor(x => x.DestinationAccountId)
            .NotEmpty()
            .WithMessage("destination_account_id is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("amount must be greater than zero.");

        RuleFor(x => x.ReferenceId)
            .NotEmpty()
            .WithMessage("reference_id is required.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .WithMessage("currency must be a valid 3-letter ISO 4217 code.");

        RuleFor(x => x)
            .Must(x => x.SourceAccountId != x.DestinationAccountId)
            .WithMessage("source and destination accounts must be different.")
            .WithName("destination_account_id");
    }
}
