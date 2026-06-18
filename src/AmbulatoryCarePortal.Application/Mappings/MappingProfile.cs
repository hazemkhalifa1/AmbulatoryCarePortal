using AutoMapper;
using AmbulatoryCarePortal.Application.DTOs.Clinic;
using AmbulatoryCarePortal.Application.DTOs.Document;
using AmbulatoryCarePortal.Application.DTOs.PolicyDocument;
using AmbulatoryCarePortal.Application.DTOs;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Clinic Mappings
        CreateMap<Clinic, CreateClinicDto>().ReverseMap();
        CreateMap<Clinic, UpdateClinicDto>().ReverseMap();
        CreateMap<Clinic, ClinicDto>();
        CreateMap<Clinic, ClinicDetailDto>();

        // PolicyDocument Mappings
        CreateMap<PolicyDocument, CreatePolicyDocumentDto>().ReverseMap();
        CreateMap<PolicyDocument, UpdatePolicyDocumentDto>().ReverseMap();
        CreateMap<PolicyDocument, PolicyDocumentDto>()
            .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department.NameEn))
            .ForMember(dest => dest.AttachmentCount, opt => opt.MapFrom(src => src.Attachments.Count));
        CreateMap<PolicyDocument, PolicyDocumentDetailDto>()
            .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department.NameEn))
            .ForMember(dest => dest.Attachments, opt => opt.MapFrom(src => src.Attachments));

        // EvidenceAttachment Mappings
        CreateMap<EvidenceAttachment, EvidenceAttachmentDto>();

        // KPI Mappings
        CreateMap<KPI, CreateKPIDto>().ReverseMap();
        CreateMap<KPI, KPIDto>()
            .ForMember(dest => dest.CurrentValue, opt => opt.MapFrom(src => 
                src.MonthlyEntries.OrderByDescending(x => x.PeriodYear)
                    .ThenByDescending(x => x.PeriodMonth)
                    .FirstOrDefault() != null ? 
                src.MonthlyEntries.OrderByDescending(x => x.PeriodYear)
                    .ThenByDescending(x => x.PeriodMonth).First().ActualValue : 0))
            .ForMember(dest => dest.Achievement, opt => opt.MapFrom(src => 
                src.MonthlyEntries.OrderByDescending(x => x.PeriodYear)
                    .ThenByDescending(x => x.PeriodMonth)
                    .FirstOrDefault() != null ? 
                (src.MonthlyEntries.OrderByDescending(x => x.PeriodYear)
                    .ThenByDescending(x => x.PeriodMonth).First().ActualValue / src.TargetValue * 100) : 0));

        // Checklist Mappings
        CreateMap<ChecklistTemplate, CreateChecklistTemplateDto>().ReverseMap();
        CreateMap<ChecklistTemplate, ChecklistTemplateDto>()
            .ForMember(dest => dest.ItemCount, opt => opt.MapFrom(src => src.Items.Count));

        CreateMap<ChecklistItem, CreateChecklistItemDto>().ReverseMap();
        CreateMap<ChecklistRound, CreateChecklistRoundDto>().ReverseMap();
        CreateMap<Domain.Entities.ChecklistAnswer, CreateChecklistAnswerDto>()
            .ForMember(dest => dest.Answer, opt => opt.MapFrom(src => src.AnswerValue))
            .ReverseMap()
            .ForMember(dest => dest.AnswerValue, opt => opt.MapFrom(src => src.Answer));
        CreateMap<ChecklistRound, ChecklistRoundDto>()
            .ForMember(dest => dest.ChecklistName, opt => opt.MapFrom(src => src.ChecklistTemplate.Name))
            .ForMember(dest => dest.ExecutedBy, opt => opt.MapFrom(src => src.ExecutedByUser!.FullNameEn))
            .ForMember(dest => dest.CompletionPercentage, opt => opt.MapFrom(src =>
                src.Answers.Count > 0
                    ? (src.Answers.Count(x => x.AnswerValue != Domain.Enums.ChecklistAnswer.NA) * 100 / src.Answers.Count)
                    : 0));

        // HR Mappings
        CreateMap<HrStaff, CreateHrStaffDto>().ReverseMap();
        CreateMap<HrStaff, HrStaffDto>()
            .ForMember(dest => dest.FullNameEn, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.FullNameEn) ? src.FullNameEn : $"{src.FirstName} {src.LastName}".Trim()))
            .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department!.NameEn))
            .ForMember(dest => dest.DocumentCount, opt => opt.MapFrom(src => src.Documents.Count));

        CreateMap<HrStaff, HrStaffDetailDto>()
            .ForMember(dest => dest.FullNameEn, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.FullNameEn) ? src.FullNameEn : $"{src.FirstName} {src.LastName}".Trim()))
            .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department!.NameEn))
            .ForMember(dest => dest.Documents, opt => opt.MapFrom(src => src.Documents));

        CreateMap<HrDocument, HrDocumentDto>()
            .ForMember(dest => dest.DaysUntilExpiry, opt => opt.MapFrom(src => 
                src.ExpiryDate.HasValue ? (int)(src.ExpiryDate.Value - DateTime.Now).TotalDays : -1));

        CreateMap<HrDocument, CreateHrDocumentDto>().ReverseMap();

        // Form Mappings
        CreateMap<Form, CreateFormDto>().ReverseMap();
        CreateMap<Form, FormDto>();

        CreateMap<FormVersion, FormVersionDto>();

        // Document Template Mappings
        CreateMap<DocumentTemplate, CreateDocumentTemplateDto>().ReverseMap();
        CreateMap<DocumentTemplate, UpdateDocumentTemplateDto>().ReverseMap();
        CreateMap<DocumentTemplate, DocumentTemplateDto>()
            .ForMember(dest => dest.ClinicType, opt => opt.MapFrom(src => src.ClinicType));

        // Document Template Version Mappings
        CreateMap<DocumentTemplateVersion, DocumentTemplateVersionDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));

        // Template Variable Mappings
        CreateMap<TemplateVariable, TemplateVariableDto>();
        CreateMap<CreateTemplateVariableDto, TemplateVariable>();
        CreateMap<UpdateTemplateVariableDto, TemplateVariable>();

        // Clinic Template Assignment Mappings
        CreateMap<ClinicTemplateAssignment, ClinicTemplateAssignmentDto>();

        // Clinic Template Value Mappings
        CreateMap<ClinicTemplateValue, ClinicTemplateValueDto>();

        // Generated Document Mappings
        CreateMap<GeneratedDocument, GeneratedDocumentDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));

        // ClinicDocument Mappings
        CreateMap<ClinicDocument, ClinicDocumentDto>();
        CreateMap<ClinicDocument, ClinicDocumentDetailDto>();

        // ClinicDocumentAttachment Mappings
        CreateMap<ClinicDocumentAttachment, ClinicDocumentAttachmentDto>();

        // Department Mappings
        CreateMap<Department, DepartmentDto>()
            .ForMember(dest => dest.ClinicName, opt => opt.MapFrom(src => src.Clinic.Name));

        CreateMap<Department, CreateDepartmentDto>().ReverseMap();

        // User Mappings
        CreateMap<AppUser, UserDto>();

        // SystemSetting Mappings
        CreateMap<SystemSetting, SystemSettingDto>();
    }
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string? FullNameEn { get; set; }
    public string? FullNameAr { get; set; }
    public string? Email { get; set; }
    public int? ClinicId { get; set; }
    public bool IsActive { get; set; }
}
