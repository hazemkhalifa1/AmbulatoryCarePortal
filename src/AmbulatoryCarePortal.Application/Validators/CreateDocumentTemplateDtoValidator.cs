using AmbulatoryCarePortal.Application.DTOs.Document;
using FluentValidation;

namespace AmbulatoryCarePortal.Application.Validators;

public class CreateDocumentTemplateDtoValidator : AbstractValidator<CreateDocumentTemplateDto>
{
    public CreateDocumentTemplateDtoValidator()
    {
        RuleFor(x => x.StandardCode)
            .NotEmpty().WithMessage("Standard code is required")
            .MaximumLength(50).WithMessage("Standard code must not exceed 50 characters");

        RuleFor(x => x.TitleEn)
            .NotEmpty().WithMessage("English title is required")
            .MaximumLength(255).WithMessage("English title must not exceed 255 characters");

        RuleFor(x => x.TitleAr)
            .MaximumLength(255).WithMessage("Arabic title must not exceed 255 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");

        RuleFor(x => x.DepartmentCategory)
            .MaximumLength(100).WithMessage("Department category must not exceed 100 characters");
    }
}
