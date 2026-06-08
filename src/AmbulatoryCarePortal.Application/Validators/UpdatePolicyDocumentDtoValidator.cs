using FluentValidation;
using AmbulatoryCarePortal.Application.DTOs.PolicyDocument;

namespace AmbulatoryCarePortal.Application.Validators;

public class UpdatePolicyDocumentDtoValidator : AbstractValidator<UpdatePolicyDocumentDto>
{
    public UpdatePolicyDocumentDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Policy document ID must be greater than 0");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Policy document title is required")
            .MaximumLength(255).WithMessage("Title must not exceed 255 characters");

        RuleFor(x => x.DocumentStatus)
            .IsInEnum().WithMessage("Invalid document status");
    }
}
