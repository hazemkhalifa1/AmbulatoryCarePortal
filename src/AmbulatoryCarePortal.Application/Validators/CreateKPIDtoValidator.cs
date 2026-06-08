using FluentValidation;
using AmbulatoryCarePortal.Application.DTOs;

namespace AmbulatoryCarePortal.Application.Validators;

public class CreateKPIDtoValidator : AbstractValidator<CreateKPIDto>
{
    public CreateKPIDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("KPI name is required")
            .MaximumLength(255).WithMessage("KPI name must not exceed 255 characters");

        RuleFor(x => x.TargetValue)
            .GreaterThan(0).WithMessage("Target value must be greater than zero");

        RuleFor(x => x.Frequency)
            .IsInEnum().WithMessage("Invalid frequency");

        RuleFor(x => x.ClinicId)
            .GreaterThan(0).WithMessage("Clinic is required");
    }
}
