using FluentValidation;
using AmbulatoryCarePortal.Application.DTOs;

namespace AmbulatoryCarePortal.Application.Validators;

public class CreateHrDocumentDtoValidator : AbstractValidator<CreateHrDocumentDto>
{
    public CreateHrDocumentDtoValidator()
    {
        RuleFor(x => x.DocumentName)
            .NotEmpty().WithMessage("Document name is required")
            .MaximumLength(255).WithMessage("Document name must not exceed 255 characters");

        RuleFor(x => x.DocumentType)
            .IsInEnum().WithMessage("Invalid document type");

        RuleFor(x => x.HrStaffId)
            .GreaterThan(0).WithMessage("Staff member is required");

        RuleFor(x => x.ExpiryDate)
            .GreaterThan(DateTime.Now)
            .WithMessage("Expiry date must be in the future")
            .When(x => x.ExpiryDate.HasValue);
    }
}
