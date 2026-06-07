using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.DTOs.EscalationRule;
using cgrmodellibrary.Exceptions;
using cgrmodellibrary.Models;
using Microsoft.EntityFrameworkCore;
using cgrdataaccesslibrary.Context;

namespace cgrbussinesslogic.Services;

public class EscalationRuleService : IEscalationRuleService
{
    private readonly IEscalationRuleRepository _ruleRepository;
    private readonly CGRContext _context;

    public EscalationRuleService(IEscalationRuleRepository ruleRepository, CGRContext context)
    {
        _ruleRepository = ruleRepository;
        _context = context;
    }

    public async Task<IEnumerable<EscalationRuleDto>> GetAllAsync(int? categoryId)
    {
        var query = _context.EscalationRules
            .Include(r => r.Category)
            .Include(r => r.Priority)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(r => r.CategoryId == categoryId.Value);

        var rules = await query.OrderBy(r => r.CategoryId).ThenBy(r => r.EscalationLevel).ToListAsync();
        return rules.Select(MapToDto);
    }

    public async Task<EscalationRuleDto> GetByIdAsync(int id)
    {
        var rule = await _context.EscalationRules
            .Include(r => r.Category)
            .Include(r => r.Priority)
            .FirstOrDefaultAsync(r => r.EscalationRuleId == id)
            ?? throw new NotFoundException($"EscalationRule with id {id}");

        return MapToDto(rule);
    }

    public async Task<EscalationRuleDto> CreateAsync(CreateEscalationRuleDto dto)
    {
        var existing = await _ruleRepository.GetRuleAsync(dto.CategoryId, dto.PriorityId, dto.EscalationLevel);
        if (existing != null)
            throw new ConflictException("An escalation rule for this category/priority/level already exists.");

        var rule = new EscalationRule
        {
            CategoryId = dto.CategoryId,
            PriorityId = dto.PriorityId,
            EscalationLevel = dto.EscalationLevel,
            EscalateAfterHours = dto.EscalateAfterHours,
           
        };

        var created = await _ruleRepository.Create(rule);
        return await GetByIdAsync(created.EscalationRuleId);
    }

    public async Task DeleteAsync(int id)
    {
        await _ruleRepository.Delete(id);
    }

    private static EscalationRuleDto MapToDto(EscalationRule r) => new()
    {
        EscalationRuleId = r.EscalationRuleId,
        CategoryId = r.CategoryId,
        CategoryName = r.Category?.CategoryName ?? string.Empty,
        PriorityId = r.PriorityId,
        PriorityName = r.Priority?.PriorityName ?? string.Empty,
        EscalationLevel = r.EscalationLevel,
        EscalateAfterHours = r.EscalateAfterHours,
    };
}
