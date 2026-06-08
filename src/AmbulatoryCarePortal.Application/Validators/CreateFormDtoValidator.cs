using FluentValidation;
using AmbulatoryCarePortal.Application.DTOs;

namespace AmbulatoryCarePortal.Application.Validators;

public class CreateFormDtoValidator : AbstractValidator<CreateFormDto>
{
    public CreateFormDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Form title is required")
            .MaximumLength(255).WithMessage("Title must not exceed 255 characters");

        RuleFor(x => x.ClinicId)
            .GreaterThan(0).WithMessage("Clinic is required");

        RuleFor(x => x.Category)
            .MaximumLength(100).WithMessage("Category must not exceed 100 characters");
    }
}
