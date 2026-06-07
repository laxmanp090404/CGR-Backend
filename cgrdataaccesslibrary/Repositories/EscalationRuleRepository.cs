using cgrdataaccesslibrary.Context;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace cgrdataaccesslibrary.Repositories;

public class EscalationRuleRepository : AbstractRepository<int, EscalationRule>, IEscalationRuleRepository
{
    private readonly CGRContext _context;

    public EscalationRuleRepository(CGRContext context) : base(context)
    {
        _context = context;
    }

    public async Task<EscalationRule?> GetRuleAsync(int categoryId, short priorityId, short escalationLevel)
    {
        return await _context.EscalationRules
            .FirstOrDefaultAsync(r => r.CategoryId == categoryId 
                                   && r.PriorityId == priorityId 
                                   && r.EscalationLevel == escalationLevel);
    }
    public async Task<IEnumerable<EscalationRule>> GetByCategoryAsync(int categoryId)
{
    return await _context.EscalationRules
        .Where(r => r.CategoryId == categoryId)
        .OrderBy(r => r.PriorityId)
        .ThenBy(r => r.EscalationLevel)
        .ToListAsync();
}
public async Task DeleteByCategoryAsync(
    int categoryId)
{
    var rules =
        await _context.EscalationRules
            .Where(r => r.CategoryId == categoryId)
            .ToListAsync();

    _context.EscalationRules.RemoveRange(rules);

}
}
