using FluentValidation;
using AmbulatoryCarePortal.Application.DTOs.Clinic;

namespace AmbulatoryCarePortal.Application.Validators;

public class UpdateClinicDtoValidator : AbstractValidator<UpdateClinicDto>
{
    public UpdateClinicDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Clinic ID must be greater than 0");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Clinic name is required")
            .MaximumLength(255).WithMessage("Clinic name must not exceed 255 characters");

        RuleFor(x => x.ClinicType)
            .IsInEnum().WithMessage("Invalid clinic type");
    }
}
