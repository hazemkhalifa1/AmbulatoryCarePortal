using FluentValidation;
using AmbulatoryCarePortal.Application.DTOs.Clinic;

namespace AmbulatoryCarePortal.Application.Validators;

public class CreateClinicDtoValidator : AbstractValidator<CreateClinicDto>
{
    public CreateClinicDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Clinic name is required")
            .MaximumLength(255).WithMessage("Clinic name must not exceed 255 characters");

        RuleFor(x => x.NameAr)
            .MaximumLength(255).WithMessage("Clinic Arabic name must not exceed 255 characters");

        RuleFor(x => x.ClinicType)
            .IsInEnum().WithMessage("Invalid clinic type");

        RuleFor(x => x.LicenseNumber)
            .MaximumLength(100).WithMessage("License number must not exceed 100 characters");

        RuleFor(x => x.LicenseExpiry)
            .GreaterThan(DateTime.Now)
            .WithMessage("License expiry must be in the future")
            .When(x => x.LicenseExpiry.HasValue);
    }
}
