using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.DTOs.Department;
using cgrmodellibrary.Exceptions;
using cgrmodellibrary.Models;

namespace cgrbussinesslogic.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IRepository<int, Department> _departmentRepository;

    public DepartmentService(IRepository<int, Department> departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    public async Task<IEnumerable<DepartmentDto>> GetAllAsync()
    {
        var departments = await _departmentRepository.GetAll();
        return departments.Select(MapToDto);
    }

    public async Task<DepartmentDto> GetByIdAsync(int id)
    {
        var dept = await _departmentRepository.Get(id);
        return MapToDto(dept);
    }

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto)
    {
        var dept = new Department
        {
            DepartmentName = dto.DepartmentName,
            DepartmentHeadEmployeeId = dto.HeadEmployeeId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var created = await _departmentRepository.Create(dept);
        return MapToDto(created);
    }

    public async Task<DepartmentDto> UpdateAsync(int id, UpdateDepartmentDto dto)
    {
        var dept = await _departmentRepository.Get(id);
        dept.DepartmentName = dto.DepartmentName;
        dept.DepartmentHeadEmployeeId = dto.HeadEmployeeId;
        var updated = await _departmentRepository.Update(dept, id);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        await _departmentRepository.Delete(id);
    }

    private static DepartmentDto MapToDto(Department d) => new()
    {
        DepartmentId = d.DepartmentId,
        DepartmentName = d.DepartmentName,
        HeadEmployeeId = d.DepartmentHeadEmployeeId,
        HeadEmployeeName = d.DepartmentHeadEmployee?.EmployeeName,
        CreatedAt = d.CreatedAt
    };
}
