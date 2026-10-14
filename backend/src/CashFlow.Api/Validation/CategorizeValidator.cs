using CashFlow.Api.Contracts;

using FluentValidation;

namespace CashFlow.Api.Validation;

public class CategorizeRequestValidator : AbstractValidator<CategorizeRequest>
{
    public CategorizeRequestValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
    }
}