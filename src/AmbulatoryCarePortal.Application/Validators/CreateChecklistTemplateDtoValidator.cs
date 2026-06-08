using FluentValidation;
using AmbulatoryCarePortal.Application.DTOs;

namespace AmbulatoryCarePortal.Application.Validators;

public class CreateChecklistTemplateDtoValidator : AbstractValidator<CreateChecklistTemplateDto>
{
    public CreateChecklistTemplateDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Template name is required")
            .MaximumLength(255).WithMessage("Template name must not exceed 255 characters");

        RuleFor(x => x.Frequency)
            .IsInEnum().WithMessage("Invalid frequency");

        RuleFor(x => x.ClinicId)
            .GreaterThan(0).WithMessage("Clinic is required");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one checklist item is required");

        RuleForEach(x => x.Items)
            .SetValidator(new CreateChecklistItemDtoValidator());
    }
}

public class CreateChecklistItemDtoValidator : AbstractValidator<CreateChecklistItemDto>
{
    public CreateChecklistItemDtoValidator()
    {
        RuleFor(x => x.Question)
            .NotEmpty().WithMessage("Question text is required")
            .MaximumLength(500).WithMessage("Question must not exceed 500 characters");
    }
}
