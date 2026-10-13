using CashFlow.Api.Contracts;

using FluentValidation;

namespace CashFlow.Api.Validation;

public class CreateScenarioRequestValidator : AbstractValidator<CreateScenarioRequest>
{
    public CreateScenarioRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.StartingBalance).InclusiveBetween(-1_000_000_000m, 1_000_000_000m);
        RuleFor(x => x.StartDate)
            .GreaterThanOrEqualTo(new DateOnly(2000, 1, 1))
            .LessThanOrEqualTo(new DateOnly(2100, 12, 31));
    }
}

public class UpdateScenarioRequestValidator : AbstractValidator<UpdateScenarioRequest>
{
    public UpdateScenarioRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.Name is not null || x.StartingBalance.HasValue || x.StartDate.HasValue)
            .WithMessage("Provide at least one field to update.");

        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).When(x => x.Name is not null);
        RuleFor(x => x.StartingBalance).InclusiveBetween(-1_000_000_000m, 1_000_000_000m);
        RuleFor(x => x.StartDate)
            .GreaterThanOrEqualTo(new DateOnly(2000, 1, 1))
            .LessThanOrEqualTo(new DateOnly(2100, 12, 31));
    }
}