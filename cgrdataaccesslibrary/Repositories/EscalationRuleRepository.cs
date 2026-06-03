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
}
