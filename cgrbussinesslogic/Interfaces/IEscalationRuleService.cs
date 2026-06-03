using cgrmodellibrary.DTOs.EscalationRule;

namespace cgrbussinesslogic.Interfaces;

public interface IEscalationRuleService
{
    Task<IEnumerable<EscalationRuleDto>> GetAllAsync(int? categoryId);
    Task<EscalationRuleDto> GetByIdAsync(int id);
    Task<EscalationRuleDto> CreateAsync(CreateEscalationRuleDto dto);
    Task DeleteAsync(int id);
}
