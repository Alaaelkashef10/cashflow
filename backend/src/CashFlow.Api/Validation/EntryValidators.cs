using CashFlow.Api.Contracts;
using CashFlow.Core.Entities;

using FluentValidation;

namespace CashFlow.Api.Validation;

public class CreateEntryRequestValidator : AbstractValidator<CreateEntryRequest>
{
    public CreateEntryRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .LessThanOrEqualTo(1_000_000_000m)
            .PrecisionScale(18, 2, true);
        RuleFor(x => x.Date)
            .GreaterThanOrEqualTo(new DateOnly(2000, 1, 1))
            .LessThanOrEqualTo(new DateOnly(2100, 12, 31));
        RuleFor(x => x.Category)
            .Must(Categories.IsValid)
            .When(x => x.Category is not null)
            .WithMessage("Unknown category. Allowed: " + string.Join(", ", Categories.All));
        RuleFor(x => x.AiSuggestedCategory)
            .Must(Categories.IsValid)
            .When(x => x.AiSuggestedCategory is not null)
            .WithMessage("Unknown category. Allowed: " + string.Join(", ", Categories.All));
    }
}

public class UpdateEntryRequestValidator : AbstractValidator<UpdateEntryRequest>
{
    public UpdateEntryRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.Type.HasValue || x.Description is not null || x.Amount.HasValue
                       || x.Date.HasValue || x.Category is not null)
            .WithMessage("Provide at least one field to update.");

        RuleFor(x => x.Type).IsInEnum().When(x => x.Type.HasValue);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(200).When(x => x.Description is not null);
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .LessThanOrEqualTo(1_000_000_000m)
            .PrecisionScale(18, 2, true);
        RuleFor(x => x.Date)
            .GreaterThanOrEqualTo(new DateOnly(2000, 1, 1))
            .LessThanOrEqualTo(new DateOnly(2100, 12, 31));
        RuleFor(x => x.Category)
            .Must(Categories.IsValid)
            .When(x => x.Category is not null)
            .WithMessage("Unknown category. Allowed: " + string.Join(", ", Categories.All));
    }
}