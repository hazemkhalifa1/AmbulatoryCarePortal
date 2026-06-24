using AmbulatoryCarePortal.Application.DTOs.Document;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace AmbulatoryCarePortal.Application.Services;

public class ClinicTemplateAssignmentService : IClinicTemplateAssignmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ClinicTemplateAssignmentService> _logger;

    public ClinicTemplateAssignmentService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<ClinicTemplateAssignmentService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<ClinicTemplateAssignmentDto>> GetAssignmentsByClinicAsync(int clinicId, string? searchTerm = null, string? statusFilter = null)
    {
        var assignments = await _unitOfWork.Repository<ClinicTemplateAssignment>()
            .FindAsync(a => a.ClinicId == clinicId);

        var result = new List<ClinicTemplateAssignmentDto>();

        foreach (var a in assignments)
        {
            var template = await _unitOfWork.Repository<DocumentTemplate>().GetByIdAsync(a.DocumentTemplateId);
            if (template == null) continue;

            if (!string.IsNullOrEmpty(searchTerm) &&
                !template.StandardCode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) &&
                !template.TitleEn.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.IsNullOrEmpty(statusFilter) &&
                !string.Equals(a.AssignmentStatus.ToString(), statusFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            var valueCount = await _unitOfWork.Repository<ClinicTemplateValue>()
                .CountAsync(v => v.ClinicTemplateAssignmentId == a.Id);
            var docCount = await _unitOfWork.Repository<GeneratedDocument>()
                .CountAsync(g => g.ClinicTemplateAssignmentId == a.Id);

            result.Add(new ClinicTemplateAssignmentDto
            {
                Id = a.Id,
                ClinicId = a.ClinicId,
                ClinicName = "",
                DocumentTemplateId = a.DocumentTemplateId,
                StandardCode = template.StandardCode,
                TitleEn = template.TitleEn,
                TitleAr = template.TitleAr,
                AssignmentStatus = a.AssignmentStatus.ToString(),
                ExpiryDate = a.ExpiryDate,
                Notes = a.Notes,
                ValueCount = valueCount,
                GeneratedDocumentCount = docCount,
                CreatedAt = a.CreatedAt
            });
        }

        return result.OrderBy(x => x.StandardCode).ToList();
    }

    public async Task<List<ClinicTemplateAssignmentDto>> GetAssignmentsByTemplateAsync(int templateId)
    {
        var assignments = await _unitOfWork.Repository<ClinicTemplateAssignment>()
            .FindAsync(a => a.DocumentTemplateId == templateId);

        var template = await _unitOfWork.Repository<DocumentTemplate>().GetByIdAsync(templateId);
        var clinicIds = assignments.Select(a => a.ClinicId).ToHashSet();
        var clinics = await _unitOfWork.Repository<Clinic>().FindAsync(c => clinicIds.Contains(c.Id));
        var clinicMap = clinics.ToDictionary(c => c.Id, c => c.Name);

        return assignments.Select(a =>
        {
            var valueCount = _unitOfWork.Repository<ClinicTemplateValue>()
                .CountAsync(v => v.ClinicTemplateAssignmentId == a.Id).GetAwaiter().GetResult();
            var docCount = _unitOfWork.Repository<GeneratedDocument>()
                .CountAsync(g => g.ClinicTemplateAssignmentId == a.Id).GetAwaiter().GetResult();

            return new ClinicTemplateAssignmentDto
            {
                Id = a.Id,
                ClinicId = a.ClinicId,
                ClinicName = clinicMap.GetValueOrDefault(a.ClinicId, "Unknown"),
                DocumentTemplateId = a.DocumentTemplateId,
                StandardCode = template?.StandardCode ?? "",
                TitleEn = template?.TitleEn ?? "",
                TitleAr = template?.TitleAr,
                AssignmentStatus = a.AssignmentStatus.ToString(),
                ExpiryDate = a.ExpiryDate,
                Notes = a.Notes,
                ValueCount = valueCount,
                GeneratedDocumentCount = docCount,
                CreatedAt = a.CreatedAt
            };
        }).OrderBy(x => x.ClinicName).ToList();
    }

    public async Task<ClinicTemplateAssignmentDto?> GetAssignmentByIdAsync(int id)
    {
        var a = await _unitOfWork.Repository<ClinicTemplateAssignment>().GetByIdAsync(id);
        if (a == null) return null;

        var template = await _unitOfWork.Repository<DocumentTemplate>().GetByIdAsync(a.DocumentTemplateId);
        var clinic = await _unitOfWork.Repository<Clinic>().GetByIdAsync(a.ClinicId);

        return new ClinicTemplateAssignmentDto
        {
            Id = a.Id,
            ClinicId = a.ClinicId,
            ClinicName = clinic?.Name ?? "",
            DocumentTemplateId = a.DocumentTemplateId,
            StandardCode = template?.StandardCode ?? "",
            TitleEn = template?.TitleEn ?? "",
            TitleAr = template?.TitleAr,
            AssignmentStatus = a.AssignmentStatus.ToString(),
            ExpiryDate = a.ExpiryDate,
            Notes = a.Notes,
            CreatedAt = a.CreatedAt
        };
    }

    public async Task AssignTemplateToClinicAsync(int templateId, int clinicId)
    {
        var existing = await _unitOfWork.Repository<ClinicTemplateAssignment>()
            .FindAsync(a => a.DocumentTemplateId == templateId && a.ClinicId == clinicId);

        if (existing.Any()) return;

        var assignment = new ClinicTemplateAssignment
        {
            ClinicId = clinicId,
            DocumentTemplateId = templateId,
            AssignmentStatus = ClinicDocumentStatus.NeedsReview,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<ClinicTemplateAssignment>().AddAsync(assignment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Template {TemplateId} assigned to clinic {ClinicId}", templateId, clinicId);
    }

    public async Task AssignTemplateToAllClinicsAsync(int templateId)
    {
        var activeClinics = await _unitOfWork.Repository<Clinic>().FindAsync(c => c.IsActive);
        var existing = await _unitOfWork.Repository<ClinicTemplateAssignment>()
            .FindAsync(a => a.DocumentTemplateId == templateId);
        var existingIds = existing.Select(a => a.ClinicId).ToHashSet();

        var newAssignments = activeClinics
            .Where(c => !existingIds.Contains(c.Id))
            .Select(c => new ClinicTemplateAssignment
            {
                ClinicId = c.Id,
                DocumentTemplateId = templateId,
                AssignmentStatus = ClinicDocumentStatus.NeedsReview,
                CreatedAt = DateTime.UtcNow
            }).ToList();

        if (newAssignments.Count == 0) return;

        await _unitOfWork.Repository<ClinicTemplateAssignment>().AddRangeAsync(newAssignments);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Template {TemplateId} assigned to {Count} clinics", templateId, newAssignments.Count);
    }

    public async Task<bool> UpdateAssignmentStatusAsync(int id, string status)
    {
        var assignment = await _unitOfWork.Repository<ClinicTemplateAssignment>().GetByIdAsync(id);
        if (assignment == null) return false;

        if (Enum.TryParse<ClinicDocumentStatus>(status, out var parsed))
        {
            assignment.AssignmentStatus = parsed;
            assignment.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<ClinicTemplateAssignment>().Update(assignment);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> UpdateAssignmentNotesAsync(int id, string? notes)
    {
        var assignment = await _unitOfWork.Repository<ClinicTemplateAssignment>().GetByIdAsync(id);
        if (assignment == null) return false;

        assignment.Notes = notes;
        assignment.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<ClinicTemplateAssignment>().Update(assignment);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAssignmentAsync(int id)
    {
        var assignment = await _unitOfWork.Repository<ClinicTemplateAssignment>().GetByIdAsync(id);
        if (assignment == null) return false;

        var values = await _unitOfWork.Repository<ClinicTemplateValue>()
            .FindAsync(v => v.ClinicTemplateAssignmentId == id);
        if (values.Any())
            _unitOfWork.Repository<ClinicTemplateValue>().SoftDeleteRange(values);

        _unitOfWork.Repository<ClinicTemplateAssignment>().SoftDelete(assignment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Clinic template assignment {AssignmentId} deleted with related values", id);
        return true;
    }

    public async Task<List<ClinicTemplateValueDto>> GetValuesForAssignmentAsync(int assignmentId)
    {
        var assignment = await _unitOfWork.Repository<ClinicTemplateAssignment>().GetByIdAsync(assignmentId);
        if (assignment == null) return new List<ClinicTemplateValueDto>();

        var variables = await _unitOfWork.Repository<TemplateVariable>()
            .FindAsync(v => v.DocumentTemplateId == assignment.DocumentTemplateId);
        var values = await _unitOfWork.Repository<ClinicTemplateValue>()
            .FindAsync(v => v.ClinicTemplateAssignmentId == assignmentId);

        var valueByVarId = values.ToDictionary(v => v.TemplateVariableId, v => v);

        return variables.Select(v => new ClinicTemplateValueDto
        {
            Id = valueByVarId.TryGetValue(v.Id, out var val) ? val.Id : 0,
            ClinicTemplateAssignmentId = assignmentId,
            TemplateVariableId = v.Id,
            VariableName = v.Name,
            DisplayName = v.DisplayName,
            IsImage = v.IsImage,
            IsRequired = v.IsRequired,
            Value = val?.Value,
            ImagePath = val?.ImagePath
        }).ToList();
    }

    public async Task<bool> UpsertValuesAsync(int assignmentId, int clinicId, List<UpsertClinicTemplateValueDto> values, string userId)
    {
        var assignment = await _unitOfWork.Repository<ClinicTemplateAssignment>().GetByIdAsync(assignmentId);
        if (assignment == null || assignment.ClinicId != clinicId) return false;

        var existingValues = await _unitOfWork.Repository<ClinicTemplateValue>()
            .FindAsync(v => v.ClinicTemplateAssignmentId == assignmentId);
        var existingByVarId = existingValues.ToDictionary(v => v.TemplateVariableId);

        foreach (var dto in values)
        {
            if (dto.Value == null) continue;

            if (existingByVarId.TryGetValue(dto.TemplateVariableId, out var existing))
            {
                existing.Value = dto.Value;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = userId;
                _unitOfWork.Repository<ClinicTemplateValue>().Update(existing);
            }
            else
            {
                var newVal = new ClinicTemplateValue
                {
                    ClinicTemplateAssignmentId = assignmentId,
                    TemplateVariableId = dto.TemplateVariableId,
                    Value = dto.Value,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<ClinicTemplateValue>().AddAsync(newVal);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Values upserted for assignment {AssignmentId}", assignmentId);
        return true;
    }

    public async Task<bool> UploadVariableImageAsync(int assignmentId, int variableId, int clinicId, string fileName, string filePath, string userId)
    {
        var assignment = await _unitOfWork.Repository<ClinicTemplateAssignment>().GetByIdAsync(assignmentId);
        if (assignment == null || assignment.ClinicId != clinicId) return false;

        var existing = await _unitOfWork.Repository<ClinicTemplateValue>()
            .FirstOrDefaultAsync(v => v.ClinicTemplateAssignmentId == assignmentId && v.TemplateVariableId == variableId);

        if (existing != null)
        {
            existing.ImagePath = filePath;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = userId;
            _unitOfWork.Repository<ClinicTemplateValue>().Update(existing);
        }
        else
        {
            var newVal = new ClinicTemplateValue
            {
                ClinicTemplateAssignmentId = assignmentId,
                TemplateVariableId = variableId,
                ImagePath = filePath,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Repository<ClinicTemplateValue>().AddAsync(newVal);
        }

        await _unitOfWork.SaveChangesAsync();
        return true;
    }


    public async Task<List<ClinicAssignmentDetailDto>> GetClinicAssignmentsWithDetailsAsync(int clinicId)
    {
        var assignments = await _unitOfWork.Repository<ClinicTemplateAssignment>()
            .FindAsync(a => a.ClinicId == clinicId);

        var signatureNames = await GetSignatureVariableNamesForClinicAsync(clinicId);

        var result = new List<ClinicAssignmentDetailDto>();

        foreach (var a in assignments)
        {
            var template = await _unitOfWork.Repository<DocumentTemplate>().GetByIdAsync(a.DocumentTemplateId);
            if (template == null) continue;

            var variables = await _unitOfWork.Repository<TemplateVariable>()
                .FindAsync(v => v.DocumentTemplateId == a.DocumentTemplateId);
            var values = await _unitOfWork.Repository<ClinicTemplateValue>()
                .FindAsync(v => v.ClinicTemplateAssignmentId == a.Id);

            var valueByVarId = values.ToDictionary(v => v.TemplateVariableId, v => v);

        var variableValues = variables
            .Where(v => !signatureNames.Contains(v.Name))
            .Select(v => new ClinicTemplateValueDto
        {
            Id = valueByVarId.TryGetValue(v.Id, out var val) ? val.Id : 0,
            ClinicTemplateAssignmentId = a.Id,
            TemplateVariableId = v.Id,
            VariableName = v.Name,
            DisplayName = v.DisplayName,
            IsImage = v.IsImage,
            IsRequired = v.IsRequired,
            Value = val?.Value,
            ImagePath = val?.ImagePath
        }).ToList();

            result.Add(new ClinicAssignmentDetailDto
            {
                AssignmentId = a.Id,
                DocumentTemplateId = a.DocumentTemplateId,
                StandardCode = template.StandardCode,
                TitleEn = template.TitleEn,
                TitleAr = template.TitleAr,
                TemplateFilePath = template.TemplateFilePath,
                AssignmentStatus = a.AssignmentStatus.ToString(),
                VariableValues = variableValues
            });
        }

        return result.OrderBy(x => x.StandardCode).ToList();
    }

    public async Task<bool> UpsertSuperAdminValuesAsync(int assignmentId, List<UpsertClinicTemplateValueDto> values, string userId)
    {
        var assignment = await _unitOfWork.Repository<ClinicTemplateAssignment>().GetByIdAsync(assignmentId);
        if (assignment == null) return false;

        var variables = await _unitOfWork.Repository<TemplateVariable>()
            .FindAsync(v => v.DocumentTemplateId == assignment.DocumentTemplateId);
        var isImageByVarId = variables.ToDictionary(v => v.Id, v => v.IsImage);

        var existingValues = await _unitOfWork.Repository<ClinicTemplateValue>()
            .FindAsync(v => v.ClinicTemplateAssignmentId == assignmentId);
        var existingByVarId = existingValues.ToDictionary(v => v.TemplateVariableId);

        foreach (var dto in values)
        {
            if (dto.Value == null) continue;

            var isImage = isImageByVarId.TryGetValue(dto.TemplateVariableId, out var img) && img;

            if (existingByVarId.TryGetValue(dto.TemplateVariableId, out var existing))
            {
                if (isImage)
                    existing.ImagePath = dto.Value;
                else
                    existing.Value = dto.Value;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = userId;
                _unitOfWork.Repository<ClinicTemplateValue>().Update(existing);
            }
            else
            {
                var newVal = new ClinicTemplateValue
                {
                    ClinicTemplateAssignmentId = assignmentId,
                    TemplateVariableId = dto.TemplateVariableId,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };
                if (isImage)
                    newVal.ImagePath = dto.Value;
                else
                    newVal.Value = dto.Value;
                await _unitOfWork.Repository<ClinicTemplateValue>().AddAsync(newVal);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Super Admin upserted values for assignment {AssignmentId}", assignmentId);
        return true;
    }

    private static readonly HashSet<string> AutoGlobalNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "ClinicName", "Clinic Name", "Clinic_Name",
        "ClinicNameAr", "Clinic Name Ar", "Clinic_Name_Ar",
        "ClinicLogo", "Clinic Logo", "Clinic_Logo",
        "Logo", "LogoPath"
    };

    private async Task<HashSet<string>> GetSignatureVariableNamesForClinicAsync(int clinicId)
    {
        var assignments = await _unitOfWork.Repository<ClinicTemplateAssignment>()
            .FindAsync(a => a.ClinicId == clinicId);

        var templateIds = assignments.Select(a => a.DocumentTemplateId).Distinct().ToList();
        if (templateIds.Count == 0)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var signatureNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var templateId in templateIds)
        {
            var templateSigners = await _unitOfWork.Repository<TemplateSigner>()
                .FindAsync(s => s.DocumentTemplateId == templateId);
            foreach (var signer in templateSigners)
            {
                signatureNames.Add($"{signer.SignerCode}_SIGNATURE");
                signatureNames.Add($"{signer.SignerCode}_NAME");
                signatureNames.Add($"{signer.SignerCode}_TITLE");
            }

            var variables = await _unitOfWork.Repository<TemplateVariable>()
                .FindAsync(v => v.DocumentTemplateId == templateId);

            var sigCodeVars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var v in variables)
            {
                if (v.Name.IndexOf("Signature", StringComparison.OrdinalIgnoreCase) >= 0)
                    signatureNames.Add(v.Name);
                if (v.Name.EndsWith("_SIGNATURE", StringComparison.OrdinalIgnoreCase))
                    sigCodeVars.Add(v.Name);
            }

            foreach (var sigVar in sigCodeVars)
            {
                var underscoreIndex = sigVar.LastIndexOf('_');
                if (underscoreIndex <= 0) continue;
                var prefix = sigVar[..underscoreIndex];
                signatureNames.Add($"{prefix}_NAME");
                signatureNames.Add($"{prefix}_TITLE");
            }
        }

        return signatureNames;
    }

    public async Task<List<GlobalTemplateValueDto>> GetGlobalTemplateValuesForClinicAsync(int clinicId)
    {
        var assignments = await _unitOfWork.Repository<ClinicTemplateAssignment>()
            .FindAsync(a => a.ClinicId == clinicId);

        if (!assignments.Any()) return new List<GlobalTemplateValueDto>();

        var allValues = new List<(TemplateVariable Variable, ClinicTemplateValue? Value)>();

        foreach (var a in assignments)
        {
            var variables = await _unitOfWork.Repository<TemplateVariable>()
                .FindAsync(v => v.DocumentTemplateId == a.DocumentTemplateId);
            var values = await _unitOfWork.Repository<ClinicTemplateValue>()
                .FindAsync(v => v.ClinicTemplateAssignmentId == a.Id);
            var valueByVarId = values.ToDictionary(v => v.TemplateVariableId);

            foreach (var v in variables)
            {
                if (valueByVarId.TryGetValue(v.Id, out var existing))
                    allValues.Add((v, existing));
                else
                    allValues.Add((v, null));
            }
        }

        var groups = allValues.GroupBy(x => x.Variable.Name, StringComparer.OrdinalIgnoreCase).ToList();

        var globalNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in groups)
        {
            var distinctTemplateIds = g.Select(x => x.Variable.DocumentTemplateId).Distinct().Count();
            if (distinctTemplateIds >= 2 || AutoGlobalNames.Contains(g.Key))
                globalNames.Add(g.Key);
        }

        var result = new List<GlobalTemplateValueDto>();
        foreach (var name in globalNames)
        {
            var entries = allValues.Where(x => string.Equals(x.Variable.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
            if (entries.Count == 0) continue;

            var first = entries.First().Variable;
            var withValue = entries.FirstOrDefault(x => x.Value != null);
            var isAuto = AutoGlobalNames.Contains(name);

            result.Add(new GlobalTemplateValueDto
            {
                VariableName = first.Name,
                DisplayName = first.DisplayName,
                IsImage = first.IsImage,
                IsRequired = first.IsRequired,
                Value = withValue.Value?.Value,
                ImagePath = withValue.Value?.ImagePath,
                IsAutoPopulated = isAuto
            });
        }

        var signatureNames = await GetSignatureVariableNamesForClinicAsync(clinicId);
        result.RemoveAll(r => signatureNames.Contains(r.VariableName));

        return result;
    }

    public async Task<bool> SaveGlobalTemplateValuesAsync(int clinicId, List<UpsertGlobalTemplateValueDto> values, string userId)
    {
        var assignments = await _unitOfWork.Repository<ClinicTemplateAssignment>()
            .FindAsync(a => a.ClinicId == clinicId);

        if (!assignments.Any()) return false;

        var assignmentTemplateIds = assignments.Select(a => a.DocumentTemplateId).Distinct().ToList();
        var allVariables = new List<TemplateVariable>();
        foreach (var templateId in assignmentTemplateIds)
        {
            var vars = await _unitOfWork.Repository<TemplateVariable>()
                .FindAsync(v => v.DocumentTemplateId == templateId);
            allVariables.AddRange(vars);
        }

        var assignmentIds = assignments.Select(a => a.Id).ToList();
        var allExistingValues = await _unitOfWork.Repository<ClinicTemplateValue>()
            .FindAsync(v => assignmentIds.Contains(v.ClinicTemplateAssignmentId));
        var existingByKey = allExistingValues.ToDictionary(v => (v.ClinicTemplateAssignmentId, v.TemplateVariableId));

        foreach (var dto in values)
        {
            if (string.IsNullOrEmpty(dto.Value)) continue;

            var matchingVars = allVariables
                .Where(v => v.Name.Equals(dto.VariableName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var variable in matchingVars)
            {
                var relevantAssignments = assignments.Where(a => a.DocumentTemplateId == variable.DocumentTemplateId);

                foreach (var assignment in relevantAssignments)
                {
                    var key = (assignment.Id, variable.Id);
                    if (existingByKey.TryGetValue(key, out var existing))
                    {
                        existing.Value = dto.Value;
                        existing.UpdatedAt = DateTime.UtcNow;
                        existing.UpdatedBy = userId;
                        _unitOfWork.Repository<ClinicTemplateValue>().Update(existing);
                    }
                    else
                    {
                        var newVal = new ClinicTemplateValue
                        {
                            ClinicTemplateAssignmentId = assignment.Id,
                            TemplateVariableId = variable.Id,
                            Value = dto.Value,
                            CreatedBy = userId,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _unitOfWork.Repository<ClinicTemplateValue>().AddAsync(newVal);
                    }
                }
            }
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Global template values saved for clinic {ClinicId}", clinicId);
        return true;
    }
}
