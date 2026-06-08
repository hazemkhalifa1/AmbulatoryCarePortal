using FluentValidation;
using AmbulatoryCarePortal.Application.DTOs;

namespace AmbulatoryCarePortal.Application.Validators;

public class CreateHrStaffDtoValidator : AbstractValidator<CreateHrStaffDto>
{
    public CreateHrStaffDtoValidator()
    {
        RuleFor(x => x.FullNameEn)
            .NotEmpty().WithMessage("Staff name is required")
            .MaximumLength(255).WithMessage("Name must not exceed 255 characters");

        RuleFor(x => x.StaffType)
            .IsInEnum().WithMessage("Invalid staff type");

        RuleFor(x => x.ClinicId)
            .GreaterThan(0).WithMessage("Clinic is required");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email format")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.NationalId)
            .MaximumLength(20).WithMessage("National ID must not exceed 20 characters");
    }
}
