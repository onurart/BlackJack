using FluentValidation;

namespace BlackJack.Validators;

public class BetRequestValidator : AbstractValidator<BetRequest>
{
    public BetRequestValidator()
    {
        RuleFor(x => x.BetAmount)
            .GreaterThan(0).WithMessage("Bet amount must be greater than zero.");
    }
}