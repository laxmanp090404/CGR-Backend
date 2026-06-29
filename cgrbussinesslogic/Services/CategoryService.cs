using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Context;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.DTOs.Category;
using cgrmodellibrary.DTOs.Common;
using cgrmodellibrary.Exceptions;
using cgrmodellibrary.Models;

namespace cgrbussinesslogic.Services;

public class CategoryService : ICategoryService
{
    private const short ROLE_ADMIN = 4;

    private readonly ICategoryRepository _categoryRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IRepository<short, Priority> _priorityRepository;
    private readonly IRepository<int, EscalationRule> _escalationRuleRepository;
    private readonly IEscalationRuleRepository _ruleRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly CGRContext _context;

    public CategoryService(
        ICategoryRepository categoryRepository,
        IDepartmentRepository departmentRepository,
        IRepository<short, Priority> priorityRepository,
        IRepository<int, EscalationRule> escalationRuleRepository,
        IEscalationRuleRepository ruleRepository,
        ICurrentUserService currentUserService,
        CGRContext context)
    {
        _categoryRepository = categoryRepository;
        _departmentRepository = departmentRepository;
        _priorityRepository = priorityRepository;
        _escalationRuleRepository = escalationRuleRepository;
        _ruleRepository = ruleRepository;
        _currentUserService = currentUserService;
        _context = context;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllAsync(bool? isActive,int? departmentId)
    {
        var categories =
            await _categoryRepository.GetByDepartmentAsync(departmentId, isActive);

        return categories.Select(MapToDto);
    }

    public async Task<PagedResultDto<CategoryDto>> GetPagedAsync(int page, int pageSize, bool? isActive, int? departmentId)
    {
        var categories =
            await _categoryRepository.GetByDepartmentAsync(departmentId, isActive);

        var list = categories.ToList();
        var totalCount = list.Count;
        var pagedItems = list
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToDto)
            .ToList();

        return new PagedResultDto<CategoryDto>
        {
            Items = pagedItems,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<CategoryDto> GetByIdAsync(int categoryId)
    {
        var category =
            await _categoryRepository.GetDetailByIdAsync(categoryId)
            ?? throw new NotFoundException($"Category {categoryId} not found.");

        return MapToDto(category);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
    {
        ValidateAdmin();

        await ValidateCategoryAsync(
            dto.CategoryName,
            dto.DepartmentId,
            dto.DefaultPriorityId);

        ValidateEscalationRules(dto.EscalationRules);

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var category =
                await _categoryRepository.Create(
                    new Category
                    {
                        CategoryName = dto.CategoryName.Trim(),
                        DepartmentId = dto.DepartmentId,
                        DefaultPriorityId = dto.DefaultPriorityId,
                        SlaHours = dto.SlaHours,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });

            foreach (var rule in dto.EscalationRules)
            {
                await _escalationRuleRepository.Create(
                    new EscalationRule
                    {
                        CategoryId = category.CategoryId,
                        PriorityId = rule.PriorityId,
                        EscalationLevel = rule.EscalationLevel,
                        EscalateAfterHours = rule.EscalateAfterHours
                    });
            }

            await transaction.CommitAsync();

            var createdCategory =
                await _categoryRepository.GetDetailByIdAsync(
                    category.CategoryId);

            return MapToDto(createdCategory!);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<CategoryDto> UpdateAsync(
        int categoryId,
        UpdateCategoryDto dto)
    {
        ValidateAdmin();

        var category =
            await _categoryRepository.GetDetailByIdAsync(categoryId)
            ?? throw new NotFoundException($"Category {categoryId} not found.");

        await ValidateCategoryAsync(
            dto.CategoryName,
            dto.DepartmentId,
            dto.DefaultPriorityId,
            categoryId);

        ValidateEscalationRules(dto.EscalationRules);

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            category.CategoryName = dto.CategoryName.Trim();
            category.DepartmentId = dto.DepartmentId;
            category.DefaultPriorityId = dto.DefaultPriorityId;
            category.SlaHours = dto.SlaHours;
            category.IsActive = dto.IsActive;
            category.UpdatedAt = DateTime.UtcNow;

            await _categoryRepository.Update(category,category.CategoryId);
            if(category.IsActive)
            {
            // only category active then rules updated
            
            var existingRules =await _ruleRepository.GetByCategoryAsync(category.CategoryId);
            if (existingRules.Count() != 8)
            {
                throw new BusinessRuleException("Category escalation rule configuration is invalid.");
            }

            foreach (var dtoRule in dto.EscalationRules)
            {
                var rule =existingRules.First(r => r.PriorityId == dtoRule.PriorityId &&
                             r.EscalationLevel == dtoRule.EscalationLevel);
                // Update escalation hours
                rule.EscalateAfterHours =
                    dtoRule.EscalateAfterHours;
                // update each rule
                await _escalationRuleRepository.Update(
                    rule,
                    rule.EscalationRuleId);
            }
            }

            await transaction.CommitAsync();

            var updated =
                await _categoryRepository.GetDetailByIdAsync(
                    category.CategoryId);

            return MapToDto(updated!);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task ValidateCategoryAsync(
        string categoryName,
        int departmentId,
        short priorityId,
        int? currentCategoryId = null)
    {
        var department =
            await _departmentRepository.GetDetailByIdAsync(departmentId);

        if (department == null)
        {
            throw new NotFoundException(
                $"Department {departmentId} not found.");
        }

        if (!department.IsActive)
        {
            throw new BusinessRuleException(
                "Department is inactive.");
        }

        var priority =
            await _priorityRepository.Get(priorityId);

        if (priority == null)
        {
            throw new NotFoundException(
                $"Priority {priorityId} not found.");
        }

        var existing =
            await _categoryRepository.GetByNameAsync(
                categoryName.Trim());

        if (existing != null &&
            existing.CategoryId != currentCategoryId)
        {
            throw new ConflictException(
                "Category name already exists.");
        }
    }

    private void ValidateEscalationRules(
        List<EscalationRuleEntryDto> rules)
    {
        if (rules.Count != 8)
        {
            throw new BusinessRuleException(
                "Exactly 8 escalation rules are required.");
        }

        var duplicates =
            rules.GroupBy(
                    x => new
                    {
                        x.PriorityId,
                        x.EscalationLevel
                    })
                .Where(g => g.Count() > 1)
                .ToList();

        if (duplicates.Any())
        {
            throw new BusinessRuleException(
                "Duplicate escalation rules found.");
        }

        var priorities =
            rules.Select(r => r.PriorityId)
                 .Distinct()
                 .OrderBy(x => x)
                 .ToList();

        if (!priorities.SequenceEqual(
                new short[] { 1, 2, 3, 4 }))
        {
            throw new BusinessRuleException(
                "Rules must be supplied for all priorities.");
        }

        foreach (var group in rules.GroupBy(x => x.PriorityId))
        {
            var level1 =
                group.FirstOrDefault(
                    x => x.EscalationLevel == 1);

            var level2 =
                group.FirstOrDefault(
                    x => x.EscalationLevel == 2);

            if (level1 == null || level2 == null)
            {
                throw new BusinessRuleException(
                    $"Priority {group.Key} must contain levels 1 and 2.");
            }

            if (level2.EscalateAfterHours >=
                level1.EscalateAfterHours)
            {
                throw new BusinessRuleException(
                    $"Level 2 escalation hours must be less than Level 1 for priority {group.Key}.");
            }
        }
    }

    private void ValidateAdmin()
    {
        if (!_currentUserService.IsAuthenticated)
        {
            throw new UnauthorizedAccessException();
        }

        if (_currentUserService.RoleId != ROLE_ADMIN)
        {
            throw new ForbiddenException(
                "Only admins can manage categories.");
        }
    }

    private static CategoryDto MapToDto(Category category)
    {
        return new CategoryDto
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            DepartmentId = category.DepartmentId,
            DepartmentName =
                category.Department?.DepartmentName ?? string.Empty,
            DefaultPriorityId = category.DefaultPriorityId,
            DefaultPriorityName =
                category.DefaultPriority?.PriorityName ?? string.Empty,
            SlaHours = category.SlaHours,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt,
            EscalationRules =
                category.EscalationRules
                    .OrderBy(x => x.PriorityId)
                    .ThenBy(x => x.EscalationLevel)
                    .Select(x => new EscalationRuleEntryDto
                    {
                        PriorityId = x.PriorityId,
                        EscalationLevel = x.EscalationLevel,
                        EscalateAfterHours = x.EscalateAfterHours
                    })
                    .ToList()
        };
    }
}