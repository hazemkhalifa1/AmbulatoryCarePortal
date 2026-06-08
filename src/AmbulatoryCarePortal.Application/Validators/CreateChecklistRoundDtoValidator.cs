using FluentValidation;
using AmbulatoryCarePortal.Application.DTOs;

namespace AmbulatoryCarePortal.Application.Validators;

public class CreateChecklistRoundDtoValidator : AbstractValidator<CreateChecklistRoundDto>
{
    public CreateChecklistRoundDtoValidator()
    {
        RuleFor(x => x.ChecklistTemplateId)
            .GreaterThan(0).WithMessage("Checklist template is required");

        RuleFor(x => x.ClinicId)
            .GreaterThan(0).WithMessage("Clinic is required");

        RuleFor(x => x.Answers)
            .NotEmpty().WithMessage("At least one answer is required");

        RuleForEach(x => x.Answers)
            .SetValidator(new CreateChecklistAnswerDtoValidator());
    }
}

public class CreateChecklistAnswerDtoValidator : AbstractValidator<CreateChecklistAnswerDto>
{
    public CreateChecklistAnswerDtoValidator()
    {
        RuleFor(x => x.ChecklistItemId)
            .GreaterThan(0).WithMessage("Checklist item is required");

        RuleFor(x => x.Answer)
            .IsInEnum().WithMessage("Invalid answer");
    }
}
