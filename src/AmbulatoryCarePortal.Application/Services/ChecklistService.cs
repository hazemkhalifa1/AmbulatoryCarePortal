using AmbulatoryCarePortal.Application.Common;
using AmbulatoryCarePortal.Application.Interfaces;
using AutoMapper;
using AmbulatoryCarePortal.Application.DTOs;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using Entities = AmbulatoryCarePortal.Domain.Entities;

namespace AmbulatoryCarePortal.Application.Services;

public class ChecklistService : IChecklistService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ChecklistService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<ChecklistTemplateDto>> GetClinicChecklistsAsync(int clinicId)
    {
        var checklists = await _unitOfWork.Repository<ChecklistTemplate>().FindAsync(x => 
            x.ClinicId == clinicId && x.IsActive
        );
        return _mapper.Map<List<ChecklistTemplateDto>>(checklists);
    }

    public async Task<int> CreateChecklistAsync(CreateChecklistTemplateDto dto)
    {
        var checklist = _mapper.Map<ChecklistTemplate>(dto);
        checklist.IsActive = true;

        await _unitOfWork.Repository<ChecklistTemplate>().AddAsync(checklist);
        await _unitOfWork.SaveChangesAsync();

        if (dto.Items.Any())
        {
            var items = dto.Items.Select(i => new ChecklistItem
            {
                ChecklistTemplateId = checklist.Id,
                QuestionText = i.Question,
                QuestionTextAr = i.QuestionAr,
                ItemOrder = i.SortOrder
            }).ToList();

            await _unitOfWork.Repository<ChecklistItem>().AddRangeAsync(items);
            await _unitOfWork.SaveChangesAsync();
        }

        return checklist.Id;
    }

    public async Task<bool> UpdateChecklistAsync(int id, CreateChecklistTemplateDto dto)
    {
        var checklist = await _unitOfWork.Repository<ChecklistTemplate>().GetByIdAsync(id);
        if (checklist == null)
            return false;

        checklist.Name = dto.Name;
        checklist.NameAr = dto.NameAr;
        checklist.Frequency = dto.Frequency;
        checklist.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<ChecklistTemplate>().Update(checklist);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteChecklistAsync(int checklistId)
    {
        var checklist = await _unitOfWork.Repository<ChecklistTemplate>().GetByIdAsync(checklistId);
        if (checklist == null)
            return false;

        _unitOfWork.Repository<ChecklistTemplate>().SoftDelete(checklist);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<int> ExecuteChecklistAsync(CreateChecklistRoundDto dto, string userId)
    {
        var round = new ChecklistRound
        {
            ChecklistTemplateId = dto.ChecklistTemplateId,
            ClinicId = dto.ClinicId,
            DepartmentId = dto.DepartmentId,
            ExecutedAt = DateTime.UtcNow,
            ExecutedByUserId = userId
        };

        await _unitOfWork.Repository<ChecklistRound>().AddAsync(round);
        await _unitOfWork.SaveChangesAsync();

        var answers = dto.Answers.Select(a => new Entities.ChecklistAnswer
        {
            ChecklistRoundId = round.Id,
            ChecklistItemId = a.ChecklistItemId,
            AnswerValue = a.Answer,
            Notes = a.Notes,
            OwnerId = userId
        }).ToList();

        await _unitOfWork.Repository<Entities.ChecklistAnswer>().AddRangeAsync(answers);
        await _unitOfWork.SaveChangesAsync();

        return round.Id;
    }

    public async Task<List<ChecklistRoundDto>> GetChecklistHistoryAsync(int templateId, int pageSize = 10)
    {
        var rounds = await _unitOfWork.Repository<ChecklistRound>().FindAsync(
            x => x.ChecklistTemplateId == templateId,
            includeDeleted: false
        );

        return _mapper.Map<List<ChecklistRoundDto>>(
            rounds.OrderByDescending(x => x.ExecutedAt).Take(pageSize)
        );
    }
}
