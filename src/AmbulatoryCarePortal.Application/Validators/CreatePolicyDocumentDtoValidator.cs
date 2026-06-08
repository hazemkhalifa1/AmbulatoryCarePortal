using FluentValidation;
using AmbulatoryCarePortal.Application.DTOs.PolicyDocument;

namespace AmbulatoryCarePortal.Application.Validators;

public class CreatePolicyDocumentDtoValidator : AbstractValidator<CreatePolicyDocumentDto>
{
    public CreatePolicyDocumentDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Policy document title is required")
            .MaximumLength(255).WithMessage("Title must not exceed 255 characters");

        RuleFor(x => x.DepartmentId)
            .GreaterThan(0).WithMessage("Department ID must be greater than 0");

        RuleFor(x => x.ClinicId)
            .GreaterThan(0).WithMessage("Clinic ID must be greater than 0");

        RuleFor(x => x.StandardCode)
            .MaximumLength(50).WithMessage("Standard code must not exceed 50 characters");

        RuleFor(x => x.ExpiryDate)
            .GreaterThan(DateTime.Now)
            .WithMessage("Expiry date must be in the future")
            .When(x => x.ExpiryDate.HasValue);
    }
}
