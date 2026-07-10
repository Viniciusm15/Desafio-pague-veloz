using FluentValidation;
using PagueVeloz.Application.Commands.Transactions;

namespace PagueVeloz.Application.Validators.Account;

public class ReversalCommandValidator : AbstractValidator<ReversalCommand>
{
    public ReversalCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("account_id is required.");

        RuleFor(x => x.OriginalOperationId)
            .NotEmpty()
            .WithMessage("original_operation_id is required.");

        RuleFor(x => x.ReferenceId)
            .NotEmpty()
            .WithMessage("reference_id is required.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .WithMessage("currency must be a valid 3-letter ISO 4217 code.");
    }
}
