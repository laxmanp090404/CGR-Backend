using cgrmodellibrary.Models;
using System.Threading.Tasks;

namespace cgrdataaccesslibrary.Interfaces;

public interface IEscalationRuleRepository : IRepository<int, EscalationRule>
{
    Task<EscalationRule?> GetRuleAsync(int categoryId, short priorityId, short escalationLevel);
}
