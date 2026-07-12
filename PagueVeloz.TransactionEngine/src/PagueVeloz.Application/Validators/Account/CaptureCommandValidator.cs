using FluentValidation;
using PagueVeloz.Application.Commands.Transactions;

namespace PagueVeloz.Application.Validators.Account;

public class CaptureCommandValidator : AbstractValidator<CaptureCommand>
{
    public CaptureCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("account_id is required.");

        RuleFor(x => x.ReserveOperationId)
            .NotEmpty()
            .WithMessage("reserve_operation_id is required.");

        RuleFor(x => x.ReferenceId)
            .NotEmpty()
            .WithMessage("reference_id is required.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .WithMessage("currency must be a valid 3-letter ISO 4217 code.");
    }
}
