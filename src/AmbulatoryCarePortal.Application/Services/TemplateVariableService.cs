using System.Text.RegularExpressions;
using AmbulatoryCarePortal.Application.DTOs.Document;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AutoMapper;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Logging;

namespace AmbulatoryCarePortal.Application.Services;

public partial class TemplateVariableService : ITemplateVariableService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<TemplateVariableService> _logger;

    [GeneratedRegex(@"\{\{(.*?)\}\}", RegexOptions.Compiled)]
    private static partial Regex PlaceholderRegex();

    public TemplateVariableService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<TemplateVariableService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<TemplateVariableDto>> GetVariablesByTemplateIdAsync(int templateId)
    {
        var variables = await _unitOfWork.Repository<TemplateVariable>()
            .FindAsync(v => v.DocumentTemplateId == templateId);
        return _mapper.Map<List<TemplateVariableDto>>(variables.ToList());
    }

    private static void ExtractFromElement(OpenXmlElement element, HashSet<string> placeholders)
    {
        if (element == null) return;

        foreach (var paragraph in element.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
        {
            var fullText = string.Concat(paragraph.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>()
                .Select(t => t.Text ?? ""));
            if (string.IsNullOrEmpty(fullText)) continue;

            var matches = PlaceholderRegex().Matches(fullText);
            foreach (Match match in matches)
                placeholders.Add(match.Groups[1].Value.Trim());
        }
    }

    public async Task<List<TemplateVariableDto>> ExtractVariablesFromFileAsync(int templateId)
    {
        var template = await _unitOfWork.Repository<DocumentTemplate>().GetByIdAsync(templateId);
        if (template == null || string.IsNullOrEmpty(template.TemplateFilePath))
            return new List<TemplateVariableDto>();

        var fullPath = template.TemplateFilePath;
        if (!(fullPath.Length >= 2 && fullPath[1] == ':' && char.IsLetter(fullPath[0])))
            fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fullPath.TrimStart('/', '\\'));

        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("Template file not found at {Path}", fullPath);
            return new List<TemplateVariableDto>();
        }

        var placeholders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using (var wordDoc = WordprocessingDocument.Open(fullPath, false))
        {
            if (wordDoc.MainDocumentPart?.Document?.Body == null)
                return new List<TemplateVariableDto>();

            ExtractFromElement(wordDoc.MainDocumentPart.Document.Body, placeholders);

            if (wordDoc.MainDocumentPart.HeaderParts != null)
            {
                foreach (var header in wordDoc.MainDocumentPart.HeaderParts)
                ExtractFromElement(header.RootElement, placeholders);
            }

            if (wordDoc.MainDocumentPart.FooterParts != null)
            {
                foreach (var footer in wordDoc.MainDocumentPart.FooterParts)
                ExtractFromElement(footer.RootElement, placeholders);
            }
        }

        var existingVariables = await _unitOfWork.Repository<TemplateVariable>()
            .FindAsync(v => v.DocumentTemplateId == templateId);
        var existingNames = existingVariables.Select(v => v.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var newVariables = new List<TemplateVariable>();
        foreach (var placeholder in placeholders)
        {
            if (existingNames.Contains(placeholder)) continue;

            var isImage = placeholder.Contains("logo", StringComparison.OrdinalIgnoreCase);

            var variable = new TemplateVariable
            {
                DocumentTemplateId = templateId,
                Name = placeholder,
                DisplayName = placeholder,
                IsImage = isImage,
                IsRequired = true
            };
            newVariables.Add(variable);
        }

        if (newVariables.Count > 0)
        {
            await _unitOfWork.Repository<TemplateVariable>().AddRangeAsync(newVariables);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Extracted {Count} variables from template {TemplateId}", newVariables.Count, templateId);
        }

        var all = await _unitOfWork.Repository<TemplateVariable>()
            .FindAsync(v => v.DocumentTemplateId == templateId);
        return _mapper.Map<List<TemplateVariableDto>>(all.ToList());
    }

    public async Task<TemplateVariableDto?> GetVariableByIdAsync(int id)
    {
        var variable = await _unitOfWork.Repository<TemplateVariable>().GetByIdAsync(id);
        return variable == null ? null : _mapper.Map<TemplateVariableDto>(variable);
    }

    public async Task<TemplateVariableDto> CreateVariableAsync(int templateId, CreateTemplateVariableDto dto)
    {
        var variable = _mapper.Map<TemplateVariable>(dto);
        variable.DocumentTemplateId = templateId;

        await _unitOfWork.Repository<TemplateVariable>().AddAsync(variable);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Variable {Name} created for template {TemplateId}", variable.Name, templateId);
        return _mapper.Map<TemplateVariableDto>(variable);
    }

    public async Task<bool> UpdateVariableAsync(UpdateTemplateVariableDto dto)
    {
        var variable = await _unitOfWork.Repository<TemplateVariable>().GetByIdAsync(dto.Id);
        if (variable == null) return false;

        _mapper.Map(dto, variable);
        variable.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<TemplateVariable>().Update(variable);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteVariableAsync(int id)
    {
        var variable = await _unitOfWork.Repository<TemplateVariable>().GetByIdAsync(id);
        if (variable == null) return false;

        _unitOfWork.Repository<TemplateVariable>().SoftDelete(variable);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<List<TemplateVariablePreviewDto>> PreviewVariableValuesAsync(int assignmentId)
    {
        var assignment = await _unitOfWork.Repository<ClinicTemplateAssignment>().GetByIdAsync(assignmentId);
        if (assignment == null) return new List<TemplateVariablePreviewDto>();

        var variables = await _unitOfWork.Repository<TemplateVariable>()
            .FindAsync(v => v.DocumentTemplateId == assignment.DocumentTemplateId);
        var values = await _unitOfWork.Repository<ClinicTemplateValue>()
            .FindAsync(v => v.ClinicTemplateAssignmentId == assignmentId);

        var valueByVarId = values.ToDictionary(v => v.TemplateVariableId, v => v);

        return variables.Select(v => new TemplateVariablePreviewDto
        {
            Name = v.Name,
            DisplayName = v.DisplayName,
            IsImage = v.IsImage,
            IsRequired = v.IsRequired,
            HasValue = valueByVarId.ContainsKey(v.Id),
            CurrentValue = valueByVarId.TryGetValue(v.Id, out var val)
                ? (val.ImagePath ?? val.Value)
                : v.DefaultValue
        }).ToList();
    }
}
